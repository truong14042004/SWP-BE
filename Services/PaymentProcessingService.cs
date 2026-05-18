using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class PaymentProcessingService(AppDbContext dbContext) : IPaymentProcessingService
{
    public async Task MarkPaidAsync(
        PaymentTransaction payment,
        DateTimeOffset paidAt,
        string? providerSubscriptionId,
        CancellationToken cancellationToken)
    {
        payment.Status = "Paid";
        payment.PaidAt ??= paidAt;
        payment.UpdatedAt = paidAt;

        if (payment.Subscription is not null)
        {
            if (payment.Subscription.Plan is null)
            {
                await dbContext.Entry(payment.Subscription)
                    .Reference(subscription => subscription.Plan)
                    .LoadAsync(cancellationToken);
            }

            var plan = payment.Subscription.Plan
                ?? throw new InvalidOperationException("Subscription plan was not found for payment processing.");

            payment.Subscription.Status = "Active";
            payment.Subscription.StartedAt ??= paidAt;
            payment.Subscription.ExpiredAt ??= CalculateExpiredAt(
                payment.Subscription.StartedAt.Value,
                plan.BillingCycle);
            payment.Subscription.ProviderSubscriptionId = string.IsNullOrWhiteSpace(providerSubscriptionId)
                ? payment.Subscription.ProviderSubscriptionId
                : providerSubscriptionId;
            payment.Subscription.UpdatedAt = paidAt;
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
                InvoiceNumber = $"INV-{payment.ProviderTransactionId ?? payment.Id.ToString("N")[..12]}",
                Amount = payment.Amount,
                Currency = payment.Currency,
                IssuedAt = paidAt,
                CreatedAt = paidAt
            });
        }
    }

    public void MarkFailed(PaymentTransaction payment, DateTimeOffset failedAt)
    {
        payment.Status = "Failed";
        payment.UpdatedAt = failedAt;

        if (payment.Subscription is not null)
        {
            payment.Subscription.Status = "PaymentFailed";
            payment.Subscription.UpdatedAt = failedAt;
        }
    }

    public DateTimeOffset CalculateExpiredAt(DateTimeOffset startedAt, string billingCycle) =>
        billingCycle.Equals("Free", StringComparison.OrdinalIgnoreCase)
            ? startedAt.AddYears(100)
            : startedAt.AddMonths(1);
}
