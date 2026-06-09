using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/subscription-plans")]
public sealed class AdminSubscriptionPlansController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionPlanResponse>>> GetPlans(
        CancellationToken cancellationToken)
    {
        var plans = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(plan => plan.Price)
            .ThenBy(plan => plan.Name)
            .Select(plan => ToResponse(plan))
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> GetPlan(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return plan is null
            ? NotFound(new { message = "Không tìm thấy gói đăng ký." })
            : Ok(ToResponse(plan));
    }

    [HttpPost]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> CreatePlan(
        SaveSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, null, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyRequest(plan, request, now);

        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPlan), new { id = plan.Id }, ToResponse(plan));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminSubscriptionPlanResponse>> UpdatePlan(
        Guid id,
        SaveSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, id, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var plan = await dbContext.SubscriptionPlans
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { message = "Không tìm thấy gói đăng ký." });
        }

        ApplyRequest(plan, request, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(plan));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await dbContext.SubscriptionPlans
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { message = "Không tìm thấy gói đăng ký." });
        }

        var isUsed = await dbContext.Subscriptions.AnyAsync(item => item.PlanId == id, cancellationToken)
            || await dbContext.PaymentTransactions.AnyAsync(item => item.PlanId == id, cancellationToken);
        if (isUsed)
        {
            plan.IsActive = false;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        dbContext.SubscriptionPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateRequest(
        SaveSubscriptionPlanRequest request,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Tên gói đăng ký là bắt buộc.";
        }

        if (request.Price < 0)
        {
            return "Giá gói đăng ký không được âm.";
        }

        if (request.MentorReviewLimit < -1)
        {
            return "Giới hạn lượt review của mentor không hợp lệ.";
        }

        if (request.AiChatLimit < -1)
        {
            return "Giới hạn lượt AI chat không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return "Đơn vị tiền tệ là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.BillingCycle))
        {
            return "Chu kỳ thanh toán là bắt buộc.";
        }

        var normalizedName = request.Name.Trim();
        var exists = await dbContext.SubscriptionPlans.AnyAsync(
            plan => plan.Name == normalizedName && plan.Id != currentId,
            cancellationToken);
        return exists ? "Tên gói đăng ký đã tồn tại." : null;
    }

    private static void ApplyRequest(
        SubscriptionPlan plan,
        SaveSubscriptionPlanRequest request,
        DateTimeOffset now)
    {
        plan.Name = request.Name.Trim();
        plan.Description = request.Description?.Trim();
        plan.Price = request.Price;
        plan.Currency = request.Currency.Trim().ToUpperInvariant();
        plan.BillingCycle = request.BillingCycle.Trim();
        plan.FeaturesJson = JsonSerializer.Serialize(new SubscriptionPlanFeatures(
            request.MentorReviewLimit,
            request.AiChatLimit,
            request.Features ?? []));
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = now;
    }

    private static AdminSubscriptionPlanResponse ToResponse(SubscriptionPlan plan)
    {
        var features = ParseFeatures(plan.FeaturesJson);
        return new AdminSubscriptionPlanResponse(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.Currency,
            plan.BillingCycle,
            features.MentorReviewLimit,
            features.AiChatLimit,
            features.Features,
            plan.IsActive,
            plan.CreatedAt,
            plan.UpdatedAt);
    }

    private static SubscriptionPlanFeatures ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return new SubscriptionPlanFeatures(0, 0, []);
        }

        try
        {
            return JsonSerializer.Deserialize<SubscriptionPlanFeatures>(
                featuresJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new SubscriptionPlanFeatures(0, 0, []);
        }
        catch (JsonException)
        {
            return new SubscriptionPlanFeatures(0, 0, []);
        }
    }
}

public sealed record SaveSubscriptionPlanRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string BillingCycle,
    int MentorReviewLimit,
    int AiChatLimit,
    IReadOnlyList<string>? Features,
    bool IsActive);

public sealed record AdminSubscriptionPlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string BillingCycle,
    int MentorReviewLimit,
    int AiChatLimit,
    IReadOnlyList<string> Features,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SubscriptionPlanFeatures(
    int MentorReviewLimit,
    int AiChatLimit,
    IReadOnlyList<string> Features);
