using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.Webhooks;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/payos")]
public sealed class PayOsController(
    AppDbContext dbContext,
    PayOSClient payOsClient,
    ILogger<PayOsController> logger) : ControllerBase
{
    private const string Provider = "PayOS";

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook(
        Webhook webhook,
        CancellationToken cancellationToken)
    {
        WebhookData webhookData;
        try
        {
            webhookData = await payOsClient.Webhooks.VerifyAsync(webhook);
        }
        catch (PayOSException exception)
        {
            logger.LogWarning(exception, "Invalid PayOS webhook signature.");
            return Unauthorized(new { message = "Invalid PayOS webhook signature." });
        }

        var payloadJson = JsonSerializer.Serialize(webhook);
        var eventId = string.IsNullOrWhiteSpace(webhookData.Reference)
            ? $"{webhookData.OrderCode}:{webhookData.Code}:{webhookData.PaymentLinkId}"
            : webhookData.Reference;

        var existingEvent = await dbContext.PaymentWebhookEvents
            .SingleOrDefaultAsync(
                item => item.Provider == Provider && item.EventId == eventId,
                cancellationToken);
        if (existingEvent is not null)
        {
            return Ok(new { success = true, message = "Webhook already processed." });
        }

        var now = DateTimeOffset.UtcNow;
        var webhookEvent = new PaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = Provider,
            EventId = eventId,
            EventType = webhookData.Code,
            PayloadJson = payloadJson,
            CreatedAt = now
        };
        dbContext.PaymentWebhookEvents.Add(webhookEvent);

        var payment = await dbContext.PaymentTransactions
            .Include(transaction => transaction.Subscription)
            .ThenInclude(subscription => subscription!.Plan)
            .SingleOrDefaultAsync(
                transaction => transaction.Provider == Provider
                    && transaction.ProviderTransactionId == webhookData.OrderCode.ToString(),
                cancellationToken);

        if (payment is null)
        {
            webhookEvent.ProcessedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { success = true, message = "Payment transaction was not found." });
        }

        if (webhookData.Code == "00")
        {
            payment.Status = "Paid";
            payment.PaidAt ??= now;

            if (payment.Subscription is not null)
            {
                payment.Subscription.Status = "Active";
                payment.Subscription.StartedAt ??= now;
                payment.Subscription.ExpiredAt ??= CalculateExpiredAt(
                    payment.Subscription.StartedAt.Value,
                    payment.Subscription.Plan.BillingCycle);
                payment.Subscription.ProviderSubscriptionId = webhookData.PaymentLinkId;
                payment.Subscription.UpdatedAt = now;
            }

            var hasInvoice = await dbContext.Invoices
                .AnyAsync(invoice => invoice.PaymentTransactionId == payment.Id, cancellationToken);
            if (!hasInvoice)
            {
                dbContext.Invoices.Add(new Invoice
                {
                    Id = Guid.NewGuid(),
                    UserId = payment.UserId,
                    PaymentTransactionId = payment.Id,
                    InvoiceNumber = $"INV-{webhookData.OrderCode}",
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    IssuedAt = now,
                    CreatedAt = now
                });
            }
        }
        else
        {
            payment.Status = "Failed";
            if (payment.Subscription is not null)
            {
                payment.Subscription.Status = "PaymentFailed";
                payment.Subscription.UpdatedAt = now;
            }
        }

        payment.UpdatedAt = now;
        webhookEvent.ProcessedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true });
    }

    private static DateTimeOffset CalculateExpiredAt(DateTimeOffset startedAt, string billingCycle) =>
        billingCycle.Equals("Free", StringComparison.OrdinalIgnoreCase)
            ? startedAt.AddYears(100)
            : startedAt.AddMonths(1);
}
