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
            limit = ParseFeatureLimit(activeSubscription.Plan?.FeaturesJson) ?? FreePlanReviewLimit;
            since = activeSubscription.StartedAt ?? activeSubscription.CreatedAt;
            periodStart = activeSubscription.StartedAt;
            periodEnd = activeSubscription.ExpiredAt;
        }

        // Used = (số portfolio feedback nhận được) + (số roadmap node review đã được approve)
        // trong period subscription hiện tại.
        var portfolioFeedbackCount = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .CountAsync(
                item => item.StudentId == studentId && item.CreatedAt >= since,
                cancellationToken);

        var approvedRoadmapReviewCount = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .CountAsync(
                item => item.StudentId == studentId
                    && item.ReviewerRole == "IndustryMentor"
                    && item.Status == "Approved"
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

        var used = portfolioFeedbackCount + approvedRoadmapReviewCount + pendingRoadmapReviewCount;

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

            foreach (var key in FeatureLimitKeys)
            {
                if (!root.TryGetProperty(key, out var element))
                {
                    continue;
                }

                var resolved = ResolveLimitValue(element);
                if (resolved.HasValue)
                {
                    return resolved.Value;
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
}
