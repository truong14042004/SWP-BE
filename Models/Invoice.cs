namespace SWP_BE.Models;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public string? PdfUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public PaymentTransaction PaymentTransaction { get; set; } = null!;
}
