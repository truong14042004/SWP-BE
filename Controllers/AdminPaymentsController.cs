using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/payments")]
public sealed class AdminPaymentsController(
    AppDbContext dbContext,
    IPaymentProcessingService paymentProcessingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPaymentTransactionResponse>>> GetPayments(
        Guid? userId,
        string? status,
        string? provider,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentTransactions
            .AsNoTracking()
            .Include(payment => payment.User)
            .Include(payment => payment.Plan)
            .Include(payment => payment.Subscription)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(payment => payment.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(payment => payment.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            var normalizedProvider = provider.Trim();
            query = query.Where(payment => payment.Provider == normalizedProvider);
        }

        if (from is not null)
        {
            query = query.Where(payment => payment.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(payment => payment.CreatedAt <= to);
        }

        var payments = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => ToPaymentResponse(payment))
            .ToListAsync(cancellationToken);

        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPaymentTransactionResponse>> GetPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.PaymentTransactions
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Plan)
            .Include(item => item.Subscription)
            .ThenInclude(subscription => subscription!.Plan)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return payment is null
            ? NotFound(new { message = "Payment transaction was not found." })
            : Ok(ToPaymentResponse(payment));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminPaymentTransactionResponse>> UpdatePaymentStatus(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new { message = "Payment status is required." });
        }

        var payment = await dbContext.PaymentTransactions
            .Include(item => item.User)
            .Include(item => item.Plan)
            .Include(item => item.Subscription)
            .ThenInclude(subscription => subscription!.Plan)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (payment is null)
        {
            return NotFound(new { message = "Payment transaction was not found." });
        }

        var now = DateTimeOffset.UtcNow;
        var normalizedStatus = request.Status.Trim();
        if (normalizedStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
        {
            await paymentProcessingService.MarkPaidAsync(payment, now, payment.Subscription?.ProviderSubscriptionId, cancellationToken);
        }
        else if (normalizedStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            || normalizedStatus.Equals("PaymentFailed", StringComparison.OrdinalIgnoreCase))
        {
            paymentProcessingService.MarkFailed(payment, now);
        }
        else
        {
            payment.Status = normalizedStatus;
            payment.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToPaymentResponse(payment));
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionResponse>>> GetSubscriptions(
        Guid? userId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions
            .AsNoTracking()
            .Include(subscription => subscription.User)
            .Include(subscription => subscription.Plan)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(subscription => subscription.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(subscription => subscription.Status == normalizedStatus);
        }

        var subscriptions = await query
            .OrderByDescending(subscription => subscription.CreatedAt)
            .Select(subscription => new AdminSubscriptionResponse(
                subscription.Id,
                subscription.UserId,
                subscription.User.Email,
                subscription.User.FullName,
                subscription.PlanId,
                subscription.Plan.Name,
                subscription.Status,
                subscription.StartedAt,
                subscription.ExpiredAt,
                subscription.CancelledAt,
                subscription.Provider,
                subscription.ProviderSubscriptionId,
                subscription.CreatedAt,
                subscription.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(subscriptions);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<AdminInvoiceResponse>>> GetInvoices(
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.User)
            .Include(invoice => invoice.PaymentTransaction)
            .AsQueryable();

        if (userId is not null)
        {
            query = query.Where(invoice => invoice.UserId == userId);
        }

        var invoices = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .Select(invoice => new AdminInvoiceResponse(
                invoice.Id,
                invoice.UserId,
                invoice.User.Email,
                invoice.User.FullName,
                invoice.PaymentTransactionId,
                invoice.InvoiceNumber,
                invoice.Amount,
                invoice.Currency,
                invoice.IssuedAt,
                invoice.PdfUrl,
                invoice.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    private static AdminPaymentTransactionResponse ToPaymentResponse(PaymentTransaction payment) =>
        new(
            payment.Id,
            payment.UserId,
            payment.User.Email,
            payment.User.FullName,
            payment.SubscriptionId,
            payment.PlanId,
            payment.Plan.Name,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.Provider,
            payment.ProviderTransactionId,
            payment.CheckoutUrl,
            payment.PaidAt,
            payment.CreatedAt,
            payment.UpdatedAt);
}

public sealed record UpdatePaymentStatusRequest(string Status);

public sealed record AdminPaymentTransactionResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    Guid? SubscriptionId,
    Guid PlanId,
    string PlanName,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? ProviderTransactionId,
    string? CheckoutUrl,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminSubscriptionResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    Guid PlanId,
    string PlanName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ExpiredAt,
    DateTimeOffset? CancelledAt,
    string? Provider,
    string? ProviderSubscriptionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminInvoiceResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    Guid PaymentTransactionId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTimeOffset IssuedAt,
    string? PdfUrl,
    DateTimeOffset CreatedAt);
