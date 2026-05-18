using SWP_BE.Models;

namespace SWP_BE.Services;

public interface IPaymentProcessingService
{
    Task MarkPaidAsync(
        PaymentTransaction payment,
        DateTimeOffset paidAt,
        string? providerSubscriptionId,
        CancellationToken cancellationToken);

    void MarkFailed(PaymentTransaction payment, DateTimeOffset failedAt);

    DateTimeOffset CalculateExpiredAt(DateTimeOffset startedAt, string billingCycle);
}
