using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class SubscriptionPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public string BillingCycle { get; set; } = null!;

    public string? FeaturesJson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
