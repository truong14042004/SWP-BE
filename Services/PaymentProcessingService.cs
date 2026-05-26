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
                ?? throw new InvalidOperationException("Không tìm thấy gói đăng ký để xử lý thanh toán.");

            // Check if user has an existing active or cancelled-but-not-expired subscription of the SAME plan
            var existingSubscription = await dbContext.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == payment.UserId 
                    && s.PlanId == plan.Id 
                    && (s.Status == "Active" || (s.Status == "Cancelled" && s.ExpiredAt > paidAt))
                    && s.Id != payment.SubscriptionId, cancellationToken);

            if (existingSubscription is not null)
            {
                // Reactivate if it was cancelled
                existingSubscription.Status = "Active";
                existingSubscription.CancelledAt = null;

                // Extend the existing active subscription
                var currentExpiredAt = existingSubscription.ExpiredAt ?? paidAt;
                if (currentExpiredAt < paidAt)
                {
                    currentExpiredAt = paidAt;
                }
                existingSubscription.ExpiredAt = CalculateExpiredAt(currentExpiredAt, plan.BillingCycle);
                existingSubscription.UpdatedAt = paidAt;

                // Mark the new subscription checkout as Cancelled (merged)
                payment.Subscription.Status = "Cancelled";
                payment.Subscription.StartedAt = paidAt;
                payment.Subscription.ExpiredAt = paidAt;
                payment.Subscription.UpdatedAt = paidAt;
            }
            else
            {
                // Normal activation
                payment.Subscription.Status = "Active";
                payment.Subscription.StartedAt ??= paidAt;
                payment.Subscription.ExpiredAt ??= CalculateExpiredAt(
                    payment.Subscription.StartedAt.Value,
                    plan.BillingCycle);
                payment.Subscription.ProviderSubscriptionId = string.IsNullOrWhiteSpace(providerSubscriptionId)
                    ? payment.Subscription.ProviderSubscriptionId
                    : providerSubscriptionId;
                payment.Subscription.UpdatedAt = paidAt;

                // Deactivate any other DIFFERENT active/cancelled/pending subscriptions
                var otherSubscriptions = await dbContext.Subscriptions
                    .Where(s => s.UserId == payment.UserId 
                        && s.Id != payment.SubscriptionId 
                        && s.PlanId != plan.Id
                        && (s.Status == "Active" || s.Status == "Pending" || (s.Status == "Cancelled" && s.ExpiredAt > paidAt)))
                    .ToListAsync(cancellationToken);

                foreach (var oldSub in otherSubscriptions)
                {
                    if (oldSub.Status == "Active" || oldSub.Status == "Cancelled")
                    {
                        oldSub.Status = "Cancelled";
                        oldSub.CancelledAt ??= paidAt;
                        oldSub.ExpiredAt = paidAt; // terminate immediately
                        oldSub.UpdatedAt = paidAt;
                    }
                    else if (oldSub.Status == "Pending")
                    {
                        oldSub.Status = "Cancelled";
                        oldSub.CancelledAt = paidAt;
                        oldSub.UpdatedAt = paidAt;
                    }
                }

                // Also cancel any other Created payment transactions for those other subscriptions
                var otherSubIds = otherSubscriptions.Select(s => s.Id).ToList();
                if (otherSubIds.Count > 0)
                {
                    var relatedPayments = await dbContext.PaymentTransactions
                        .Where(p => p.SubscriptionId.HasValue 
                            && otherSubIds.Contains(p.SubscriptionId.Value) 
                            && p.Status == "Created")
                        .ToListAsync(cancellationToken);

                    foreach (var relPayment in relatedPayments)
                    {
                        relPayment.Status = "Cancelled";
                        relPayment.UpdatedAt = paidAt;
                    }
                }
            }
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

    public DateTimeOffset CalculateExpiredAt(DateTimeOffset startedAt, string billingCycle)
    {
        if (billingCycle.Equals("Free", StringComparison.OrdinalIgnoreCase))
        {
            return startedAt.AddYears(100);
        }
        if (billingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
        {
            return startedAt.AddYears(1);
        }
        if (billingCycle.Equals("Quarterly", StringComparison.OrdinalIgnoreCase))
        {
            return startedAt.AddMonths(3);
        }
        // Default to Monthly
        return startedAt.AddMonths(1);
    }
}
