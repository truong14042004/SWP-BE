using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api")]
public sealed class SubscriptionsController(
    AppDbContext dbContext,
    PayOSClient payOsClient,
    IOptions<PayOsOptions> payOsOptions,
    IPaymentProcessingService paymentProcessingService) : ControllerBase
{
    private const string Provider = "PayOS";

    [HttpGet("subscription-plans")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> GetPlans(
        CancellationToken cancellationToken)
    {
        var plans = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Price)
            .Select(plan => new SubscriptionPlanResponse(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.Price,
                plan.Currency,
                plan.BillingCycle,
                plan.FeaturesJson))
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [Authorize]
    [HttpPost("subscriptions/checkout")]
    public async Task<ActionResult<SubscriptionCheckoutResponse>> CreateCheckout(
        CreateSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var plan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive, cancellationToken);

        if (plan is null)
        {
            return NotFound(new { message = "Không tìm thấy gói đăng ký." });
        }

        if (!string.Equals(plan.Currency, "VND", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Thanh toán qua PayOS hiện chỉ hỗ trợ các gói bằng VND." });
        }

        var amount = decimal.ToInt32(decimal.Round(plan.Price, 0, MidpointRounding.AwayFromZero));
        if (amount < 0)
        {
            return BadRequest(new { message = "Giá của gói đăng ký không thể là số âm." });
        }

        if (amount == 0)
        {
            var existingFreeSubscription = await dbContext.Subscriptions
                .Include(subscription => subscription.Plan)
                .Where(subscription => subscription.UserId == userId
                    && subscription.PlanId == plan.Id
                    && subscription.Status == "Active")
                .OrderByDescending(subscription => subscription.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingFreeSubscription is not null)
            {
                return Ok(new SubscriptionCheckoutResponse(
                    null,
                    existingFreeSubscription.Id,
                    null,
                    0,
                    plan.Currency,
                    existingFreeSubscription.Status,
                    string.Empty));
            }

            var activatedAt = DateTimeOffset.UtcNow;
            var freeSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Status = "Active",
                StartedAt = activatedAt,
                ExpiredAt = paymentProcessingService.CalculateExpiredAt(activatedAt, plan.BillingCycle),
                Provider = "Free",
                CreatedAt = activatedAt,
                UpdatedAt = activatedAt
            };
            dbContext.Subscriptions.Add(freeSubscription);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new SubscriptionCheckoutResponse(
                null,
                freeSubscription.Id,
                null,
                0,
                plan.Currency,
                freeSubscription.Status,
                string.Empty));
        }

        var now = DateTimeOffset.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            Status = "Pending",
            Provider = Provider,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Subscriptions.Add(subscription);

        var orderCode = CreateOrderCode();
        var payment = new SWP_BE.Models.PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = subscription.Id,
            PlanId = plan.Id,
            Amount = amount,
            Currency = "VND",
            Status = "Created",
            Provider = Provider,
            ProviderTransactionId = orderCode.ToString(),
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.PaymentTransactions.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var options = payOsOptions.Value;
        try
        {
            var paymentLink = await payOsClient.PaymentRequests.CreateAsync(
                new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = amount,
                    Description = $"CareerMap {orderCode}",
                    ReturnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? options.ReturnUrl : request.ReturnUrl,
                    CancelUrl = string.IsNullOrWhiteSpace(request.CancelUrl) ? options.CancelUrl : request.CancelUrl
                },
                new RequestOptions<CreatePaymentLinkRequest>
                {
                    CancellationToken = cancellationToken
                });

            payment.CheckoutUrl = paymentLink.CheckoutUrl;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            var failedAt = DateTimeOffset.UtcNow;
            payment.Status = "PaymentLinkFailed";
            payment.UpdatedAt = failedAt;
            subscription.Status = "CheckoutFailed";
            subscription.UpdatedAt = failedAt;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return Ok(new SubscriptionCheckoutResponse(
            payment.Id,
            subscription.Id,
            orderCode,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.CheckoutUrl ?? string.Empty));
    }

    [Authorize]
    [HttpGet("subscriptions/me")]
    public async Task<ActionResult<IReadOnlyList<MySubscriptionResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Plan)
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .Select(subscription => new MySubscriptionResponse(
                subscription.Id,
                subscription.PlanId,
                subscription.Plan.Name,
                subscription.Status,
                subscription.StartedAt,
                subscription.ExpiredAt,
                subscription.CancelledAt,
                subscription.Provider,
                subscription.CreatedAt,
                subscription.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(subscriptions);
    }

    [Authorize]
    [HttpPost("subscriptions/cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var subscription = await dbContext.Subscriptions
            .Where(item => item.UserId == userId && item.Status == "Active")
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return NotFound(new { message = "Không tìm thấy gói đăng ký đang hoạt động." });
        }

        var now = DateTimeOffset.UtcNow;
        subscription.Status = "Cancelled";
        subscription.CancelledAt = now;
        subscription.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }

    private static long CreateOrderCode()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var suffix = Random.Shared.Next(100, 999);
        return long.Parse($"{timestamp}{suffix}"[^15..]);
    }
}

public sealed record CreateSubscriptionCheckoutRequest(
    Guid PlanId,
    string? ReturnUrl,
    string? CancelUrl);

public sealed record SubscriptionPlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string BillingCycle,
    string? FeaturesJson);

public sealed record SubscriptionCheckoutResponse(
    Guid? PaymentTransactionId,
    Guid SubscriptionId,
    long? OrderCode,
    decimal Amount,
    string Currency,
    string Status,
    string CheckoutUrl);

public sealed record MySubscriptionResponse(
    Guid Id,
    Guid PlanId,
    string PlanName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ExpiredAt,
    DateTimeOffset? CancelledAt,
    string? Provider,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
