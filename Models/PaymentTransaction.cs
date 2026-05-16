namespace SWP_BE.Models;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string Provider { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Subscription? Subscription { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;
}
