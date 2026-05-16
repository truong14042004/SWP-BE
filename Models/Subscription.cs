using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Subscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Provider { get; set; }

    public string? ProviderSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
