using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-mentor")]
public sealed class AiMentorActionsController(AppDbContext dbContext) : ControllerBase
{
    [Authorize(Roles = UserRoles.Student)]
    [HttpPost("apply-roadmap")]
    public async Task<ActionResult> ApplyRoadmap(
        ApplyAiRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roadmap is null)
        {
            return BadRequest(new { message = "Nội dung lộ trình là bắt buộc." });
        }

        var userId = GetCurrentUserId();

        // Check if student has an active counselor assigned
        var counselorId = await dbContext.CounselorAssignments
            .AsNoTracking()
            .Where(a => a.StudentId == userId && a.Status == "Active")
            .Select(a => (Guid?)a.CounselorId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!counselorId.HasValue)
        {
            return BadRequest(new { message = "Bạn chưa được phân công cố vấn học tập để gửi đề xuất phê duyệt lộ trình. Vui lòng liên hệ quản trị viên." });
        }

        // Validate that roadmap is not empty/invalid
        var skills = await dbContext.Skills
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Id, item.Name, item.Category })
            .ToListAsync(cancellationToken);
        var categoryTitles = skills.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var categorySet = categoryTitles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skillIdsByTitle = skills
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        var resources = await dbContext.LearningResources
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Title })
            .ToListAsync(cancellationToken);
        var resourceIdsByTitle = resources
            .GroupBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Title, StringComparer.OrdinalIgnoreCase);

        var allowedModuleTitles = skillIdsByTitle.Keys
            .Concat(resourceIdsByTitle.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sanitizedNodes = Services.RoadmapMaterializer.SanitizeRoadmapCategories(request.Roadmap.Nodes, categorySet, allowedModuleTitles);
        if (sanitizedNodes.Count == 0)
        {
            return BadRequest(new { message = "Lộ trình phải chứa ít nhất một module hợp lệ." });
        }

        var now = DateTimeOffset.UtcNow;
        var approvalRequest = new RoadmapApprovalRequest
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            CounselorId = counselorId.Value,
            Status = "Pending",
            PayloadJson = JsonSerializer.Serialize(request.Roadmap),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.RoadmapApprovalRequests.Add(approvalRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { requestId = approvalRequest.Id });
    }

    [HttpGet("roadmap-approval-requests")]
    public async Task<ActionResult<IReadOnlyList<StudentRoadmapApprovalRequestResponse>>> GetStudentRoadmapApprovalRequests(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var requests = await dbContext.RoadmapApprovalRequests
            .AsNoTracking()
            .Include(r => r.MaterializedRoadmap)
            .Where(r => r.StudentId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = requests.Select(r =>
        {
            string? title = null;
            try
            {
                using var doc = JsonDocument.Parse(r.PayloadJson);
                if (doc.RootElement.TryGetProperty("Title", out var titleProp))
                {
                    title = titleProp.GetString();
                }
            }
            catch { /* ignore parsing */ }

            return new StudentRoadmapApprovalRequestResponse(
                r.Id,
                r.Status,
                title ?? "Lộ trình đề xuất",
                r.RejectionReason,
                r.MaterializedRoadmapId,
                r.MaterializedRoadmap?.Title,
                r.CreatedAt,
                r.UpdatedAt);
        }).ToList();

        return Ok(response);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token người dùng không hợp lệ.");
    }
}

public sealed record StudentRoadmapApprovalRequestResponse(
    Guid Id,
    string Status,
    string ProposedTitle,
    string? RejectionReason,
    Guid? MaterializedRoadmapId,
    string? MaterializedRoadmapTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ApplyAiRoadmapRequest(AiRoadmapDto Roadmap);

public sealed record AiRoadmapDto(
    string Title,
    string? Description,
    string? CareerRoleHint,
    int? TotalEstimatedHours,
    IReadOnlyList<AiRoadmapNodeDto>? Nodes);

public sealed record AiRoadmapNodeDto(
    string Title,
    string? Description,
    string? NodeType,
    int? EstimatedHours,
    int? Priority,
    int? OrderIndex,
    IReadOnlyList<AiRoadmapNodeDto>? Children);

public sealed record ApplyAiRoadmapResponse(
    Guid RoadmapId,
    string Title,
    int NodeCount,
    bool IsExisting);
