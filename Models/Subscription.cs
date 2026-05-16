namespace SWP_BE.Models;

public sealed class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? Provider { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
