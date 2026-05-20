using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Student)]
[Route("api/student")]
public sealed class StudentFeedbacksController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("feedbacks")]
    public async Task<ActionResult<StudentFeedbacksResponse>> GetMyFeedbacks(CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var counselorItems = await dbContext.CounselorFeedbacks
            .AsNoTracking()
            .Include(item => item.Counselor)
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new StudentFeedbackItem(
                item.Id,
                "Counselor",
                item.Counselor.Id,
                item.Counselor.FullName,
                item.Counselor.Email,
                item.Counselor.AvatarUrl,
                item.FeedbackText,
                item.Rating,
                item.Recommendations,
                null,
                null,
                null,
                item.RoadmapId,
                item.SkillGapReportId,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        var mentorItems = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .Include(item => item.Mentor)
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new StudentFeedbackItem(
                item.Id,
                "IndustryMentor",
                item.Mentor.Id,
                item.Mentor.FullName,
                item.Mentor.Email,
                item.Mentor.AvatarUrl,
                item.Comment,
                item.Rating,
                item.Recommendations,
                item.PortfolioQualityFeedback,
                item.TechnicalSkillsAssessment,
                item.JobReadinessLevel,
                null,
                null,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        // Counselors are typically multi-feedback per student; collapse to a sorted timeline
        var combined = counselorItems
            .Concat(mentorItems)
            .OrderByDescending(item => item.CreatedAt)
            .ToList();

        return Ok(new StudentFeedbacksResponse(
            combined,
            counselorItems.Count,
            mentorItems.Count));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }
}

public sealed record StudentFeedbackItem(
    Guid Id,
    string Source,                      // "Counselor" | "IndustryMentor"
    Guid AuthorId,
    string AuthorFullName,
    string AuthorEmail,
    string? AuthorAvatarUrl,
    string Comment,
    int? Rating,
    string? Recommendations,
    string? PortfolioQualityFeedback,
    string? TechnicalSkillsAssessment,
    string? JobReadinessLevel,
    Guid? RoadmapId,
    Guid? SkillGapReportId,
    DateTimeOffset CreatedAt);

public sealed record StudentFeedbacksResponse(
    IReadOnlyList<StudentFeedbackItem> Items,
    int CounselorCount,
    int MentorCount);
