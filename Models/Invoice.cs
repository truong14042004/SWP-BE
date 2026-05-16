using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Invoice
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid PaymentTransactionId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public string? PdfUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
