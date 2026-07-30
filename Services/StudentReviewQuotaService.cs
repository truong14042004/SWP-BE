using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;

namespace SWP_BE.Services;

public sealed record StudentReviewQuota(
    string PlanName,
    int Limit,
    int Used,
    int Remaining,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd);

public interface IStudentReviewQuotaService
{
    Task<StudentReviewQuota> GetQuotaAsync(Guid studentId, CancellationToken cancellationToken);
}

/// <summary>
/// Tính quota review của 1 student theo subscription plan đang active.
/// "Used" gộp cả portfolio feedback (MentorFeedback) và roadmap node approve
/// — bất kỳ lượt review nào mentor xử lý cho student đều trừ vào quota.
/// </summary>
public sealed class StudentReviewQuotaService(AppDbContext dbContext) : IStudentReviewQuotaService
{
    private const int FreePlanReviewLimit = 2;
    private const string FreePlanName = "Free";

    // Các key có thể xuất hiện trong FeaturesJson cho hạn mức review.
    // Tên cố định để tránh phân mảnh schema giữa nhiều plan version.
    private static readonly string[] FeatureLimitKeys =
    [
        "mentorReviewLimit",
        "mentorReviews",
        "mentorReviewsPerMonth",
        "mentor_review_limit",
        "reviewLimit",
        "reviews"
    ];

    public async Task<StudentReviewQuota> GetQuotaAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSubscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(item => item.Plan)
            .Where(item => item.UserId == studentId && (item.Status == "Active" || (item.Status == "Cancelled" && item.ExpiredAt > now)))
            .OrderByDescending(item => item.StartedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var planName = FreePlanName;
        var limit = FreePlanReviewLimit;
        DateTimeOffset since = DateTimeOffset.MinValue;
        DateTimeOffset? periodStart = null;
        DateTimeOffset? periodEnd = null;

        if (activeSubscription is not null)
        {
            planName = activeSubscription.Plan?.Name ?? FreePlanName;
            var baseLimit = ParseFeatureLimit(activeSubscription.Plan?.FeaturesJson) ?? FreePlanReviewLimit;
            since = activeSubscription.StartedAt ?? activeSubscription.CreatedAt;
            periodStart = activeSubscription.StartedAt;
            periodEnd = activeSubscription.ExpiredAt;

            if (activeSubscription.StartedAt.HasValue && activeSubscription.ExpiredAt.HasValue)
            {
                var billingCycle = activeSubscription.Plan?.BillingCycle ?? "Monthly";
                var periods = CalculatePeriods(activeSubscription.StartedAt.Value, activeSubscription.ExpiredAt.Value, billingCycle);
                limit = baseLimit == int.MaxValue ? int.MaxValue : baseLimit * periods;
            }
            else
            {
                limit = baseLimit;
            }
        }

        // Used = (số portfolio feedback nhận được) + (số roadmap node review mentor đã
        // xử lý: approve hoặc reject) + (số đang chờ) trong period subscription hiện tại.
        var portfolioFeedbackCount = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .CountAsync(
                item => item.StudentId == studentId && item.CreatedAt >= since,
                cancellationToken);

        // Tính cả lượt bị từ chối: mentor đã bỏ công xem xét thì lượt đó đã tiêu thụ
        // quota. Nếu chỉ đếm Approved, sinh viên bị reject sẽ được hoàn quota và có
        // thể gửi lại vô hạn (node quay về "Completed" nên gửi lại được ngay).
        // Chỉ Cancelled mới được hoàn — mentor chưa xử lý.
        var handledRoadmapReviewCount = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .CountAsync(
                item => item.StudentId == studentId
                    && item.ReviewerRole == "IndustryMentor"
                    && (item.Status == "Approved" || item.Status == "Rejected")
                    && (item.RespondedAt ?? item.RequestedAt) >= since,
                cancellationToken);

        var pendingRoadmapReviewCount = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .CountAsync(
                item => item.StudentId == studentId
                    && item.ReviewerRole == "IndustryMentor"
                    && item.Status == "Pending"
                    && item.RequestedAt >= since,
                cancellationToken);

        var used = portfolioFeedbackCount + handledRoadmapReviewCount + pendingRoadmapReviewCount;

        return new StudentReviewQuota(
            planName,
            limit,
            used,
            Math.Max(limit - used, 0),
            periodStart,
            periodEnd);
    }

    /// <summary>
    /// Parse features JSON của plan cho giới hạn review.
    /// Hỗ trợ cả schema lồng (object) và schema phẳng (number/string).
    /// Nếu plan có "unlimited"/"-1" → trả int.MaxValue.
    /// </summary>
    private static int? ParseFeatureLimit(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(featuresJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // So khớp key KHÔNG phân biệt hoa thường: dữ liệu plan thực tế đang lẫn
            // "mentorReviewLimit" (Free) và "MentorReviewLimit" (Premium) — TryGetProperty
            // case-sensitive từng làm gói trả phí rơi về hạn mức Free.
            foreach (var key in FeatureLimitKeys)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var resolved = ResolveLimitValue(property.Value);
                    if (resolved.HasValue)
                    {
                        return resolved.Value;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static int? ResolveLimitValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetInt32(out var number):
                return number;

            case JsonValueKind.String:
                var raw = element.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }
                if (raw.Equals("unlimited", StringComparison.OrdinalIgnoreCase)
                    || raw == "-1")
                {
                    return int.MaxValue;
                }
                return int.TryParse(raw, out var parsed) ? parsed : null;

            default:
                return null;
        }
    }

    private static int CalculatePeriods(DateTimeOffset start, DateTimeOffset end, string billingCycle)
    {
        if (billingCycle.Equals("Free", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (months <= 0)
        {
            return 1;
        }

        if (billingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, months / 12);
        }
        if (billingCycle.Equals("Quarterly", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, months / 3);
        }

        // Default to Monthly
        return months;
    }
}
