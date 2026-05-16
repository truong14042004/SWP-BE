using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class PaymentWebhookEvent
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
