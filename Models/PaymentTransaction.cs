using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public Guid PlanId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string? ProviderTransactionId { get; set; }

    public string? CheckoutUrl { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual Subscription? Subscription { get; set; }

    public virtual User User { get; set; } = null!;
}
