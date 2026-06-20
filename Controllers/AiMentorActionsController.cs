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
public sealed class AiMentorActionsController(
    AppDbContext dbContext,
    ILogger<AiMentorActionsController> logger) : ControllerBase
{
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

    private static IReadOnlyList<AiRoadmapNodeDto> SanitizeRoadmapCategories(
        IReadOnlyList<AiRoadmapNodeDto>? source,
        HashSet<string> categorySet,
        HashSet<string> allowedModuleTitles)
    {
        if (source is null) return [];

        var output = new List<AiRoadmapNodeDto>();
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title) || !categorySet.Contains(item.Title)) continue;

            output.Add(item with
            {
                Title = item.Title.Trim(),
                NodeType = "Group",
                Children = SanitizeRoadmapModules(item.Children, allowedModuleTitles)
            });
        }

        return output;
    }

    private static IReadOnlyList<AiRoadmapNodeDto> SanitizeRoadmapModules(
        IReadOnlyList<AiRoadmapNodeDto>? source,
        HashSet<string> allowedModuleTitles)
    {
        if (source is null) return [];

        var output = new List<AiRoadmapNodeDto>();
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title)) continue;

            var children = SanitizeRoadmapModules(item.Children, allowedModuleTitles);
            if (!allowedModuleTitles.Contains(item.Title))
            {
                output.AddRange(children);
                continue;
            }

            output.Add(item with
            {
                Title = item.Title.Trim(),
                NodeType = children.Count > 0 ? "Group" : "Module",
                Children = children
            });
        }

        return output;
    }

    private static void FlattenNodes(
        IReadOnlyList<AiRoadmapNodeDto>? source,
        Guid roadmapId,
        Guid? parentId,
        int level,
        ref int globalOrder,
        DateTimeOffset now,
        List<RoadmapNode> output,
        List<RoadmapNodeResource> nodeResources,
        IReadOnlyDictionary<string, Guid> skillIdsByTitle,
        IReadOnlyDictionary<string, Guid> resourceIdsByTitle,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> resourceIdsBySkill)
    {
        if (source is null) return;

        // Hard caps to satisfy DB check constraints.
        // Level: 0..8 (CK_roadmap_nodes_Level)
        // Priority: 1..5 (CK_roadmap_nodes_Priority)
        var safeLevel = Math.Clamp(level, 0, 8);

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title)) continue;
            var title = item.Title.Trim();

            var nodeType = string.IsNullOrWhiteSpace(item.NodeType)
                ? (item.Children?.Count > 0 ? "Group" : "Module")
                : item.NodeType.Trim();

            Guid? skillId = null;
            var relatedResourceIds = new List<Guid>();
            if (skillIdsByTitle.TryGetValue(title, out var matchedSkillId))
            {
                skillId = matchedSkillId;
                if (resourceIdsBySkill.TryGetValue(matchedSkillId, out var skillResourceIds))
                {
                    relatedResourceIds.AddRange(skillResourceIds);
                }
            }
            else if (resourceIdsByTitle.TryGetValue(title, out var matchedResourceId))
            {
                relatedResourceIds.Add(matchedResourceId);
            }

            // Squash any AI-supplied 1-10 priority down to 1-5.
            int priority = item.Priority switch
            {
                null or < 1 => 3,
                <= 5 => item.Priority.Value,
                <= 10 => (int)Math.Ceiling(item.Priority.Value / 2.0),
                _ => 5
            };

            var node = new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmapId,
                SkillId = skillId,
                LearningResourceId = relatedResourceIds.Select(resourceId => (Guid?)resourceId).FirstOrDefault(),
                ParentNodeId = parentId,
                Title = title,
                Description = item.Description?.Trim(),
                NodeType = nodeType,
                Status = "NotStarted",
                Level = safeLevel,
                OrderIndex = globalOrder,
                EstimatedHours = item.EstimatedHours.HasValue && item.EstimatedHours.Value > 0
                    ? item.EstimatedHours.Value
                    : null,
                Priority = priority,
                CreatedAt = now,
                UpdatedAt = now
            };
            output.Add(node);
            nodeResources.AddRange(relatedResourceIds
                .Distinct()
                .Select((resourceId, resourceIndex) => new RoadmapNodeResource
                {
                    Id = Guid.NewGuid(),
                    RoadmapNodeId = node.Id,
                    LearningResourceId = resourceId,
                    OrderIndex = resourceIndex + 1,
                    CreatedAt = now
                }));
            globalOrder++;

            FlattenNodes(
                item.Children,
                roadmapId,
                node.Id,
                level + 1,
                ref globalOrder,
                now,
                output,
                nodeResources,
                skillIdsByTitle,
                resourceIdsByTitle,
                resourceIdsBySkill);
        }
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
