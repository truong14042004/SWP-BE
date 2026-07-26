using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.Webhooks;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/payos")]
public sealed class PayOsController(
    AppDbContext dbContext,
    PayOSClient payOsClient,
    IPaymentProcessingService paymentProcessingService,
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
            return Unauthorized(new { message = "Chữ ký webhook PayOS không hợp lệ." });
        }

        try
        {
            await ProcessWebhook(webhook, webhookData, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "PayOS webhook was verified but processing failed. OrderCode={OrderCode} Reference={Reference}",
                webhookData.OrderCode,
                webhookData.Reference);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Xử lý webhook PayOS thất bại." });
        }

        return Ok(new { success = true });
    }

    private async Task ProcessWebhook(
        Webhook webhook,
        WebhookData webhookData,
        CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(webhook);
        var eventId = !string.IsNullOrWhiteSpace(webhookData.Reference)
            ? webhookData.Reference
            : !string.IsNullOrWhiteSpace(webhookData.PaymentLinkId)
                ? webhookData.PaymentLinkId
                : $"{webhookData.OrderCode}:{webhookData.Code ?? webhook.Code ?? "Unknown"}";
        var eventType = webhookData.Code ?? webhook.Code ?? "Unknown";

        var existingEvent = await dbContext.PaymentWebhookEvents
            .SingleOrDefaultAsync(
                item => item.Provider == Provider && item.EventId == eventId,
                cancellationToken);
        if (existingEvent is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var webhookEvent = new PaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = Provider,
            EventId = eventId,
            EventType = eventType,
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
            if (IsLikelyVerificationWebhook(webhookData))
            {
                webhookEvent.ProcessedAt = now;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            throw new InvalidOperationException($"Không tìm thấy giao dịch thanh toán cho mã đơn hàng PayOS {webhookData.OrderCode}.");
        }

        if (webhookData.Code == "00")
        {
            // Phòng vệ chiều sâu: chữ ký đã được verify ở trên, nhưng vẫn phải đối chiếu
            // số tiền — không có bước này thì một payload báo thành công với số tiền lệch
            // vẫn kích hoạt gói đầy đủ quyền lợi.
            var expectedAmount = decimal.ToInt32(decimal.Round(payment.Amount, 0, MidpointRounding.AwayFromZero));
            if (webhookData.Amount != expectedAmount)
            {
                paymentProcessingService.MarkFailed(payment, now);
                logger.LogWarning(
                    "Webhook PayOS báo thành công nhưng số tiền không khớp cho đơn {OrderCode}: nhận {Received}, cần {Expected}.",
                    webhookData.OrderCode,
                    webhookData.Amount,
                    expectedAmount);
            }
            else
            {
                await paymentProcessingService.MarkPaidAsync(
                    payment,
                    now,
                    webhookData.PaymentLinkId,
                    cancellationToken);
            }
        }
        else
        {
            paymentProcessingService.MarkFailed(payment, now);
        }

        payment.UpdatedAt = now;
        webhookEvent.ProcessedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Order code thật được sinh ở SubscriptionsController.CreateOrderCode() và luôn là số dương 15 chữ số.
    // Webhook xác minh/test của PayOS có OrderCode = 0 (hoặc âm), nên chỉ cần dựa vào OrderCode <= 0.
    // Bỏ điều kiện độ dài < 13 cũ vì dễ vỡ và có nguy cơ nuốt nhầm webhook thật.
    private static bool IsLikelyVerificationWebhook(WebhookData webhookData) =>
        webhookData.OrderCode <= 0;
}
