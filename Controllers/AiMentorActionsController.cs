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
    public async Task<ActionResult<ApplyAiRoadmapResponse>> ApplyRoadmap(
        ApplyAiRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roadmap is null)
        {
            return BadRequest(new { message = "Nội dung lộ trình là bắt buộc." });
        }

        var userId = GetCurrentUserId();

        // Find a career role to attach: prefer the role the AI hinted at, else student profile target role,
        // else fall back to any active role.
        Guid? targetRoleId = null;

        if (!string.IsNullOrWhiteSpace(request.Roadmap.CareerRoleHint))
        {
            var hint = request.Roadmap.CareerRoleHint.Trim().ToLowerInvariant();
            targetRoleId = await dbContext.CareerRoles
                .AsNoTracking()
                .Where(role => role.IsActive && role.Name.ToLower().Contains(hint))
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!targetRoleId.HasValue)
        {
            var profile = await dbContext.StudentProfiles
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .Select(item => new { item.TargetRoleId })
                .SingleOrDefaultAsync(cancellationToken);

            if (profile?.TargetRoleId is Guid roleId)
            {
                targetRoleId = roleId;
            }
        }

        if (!targetRoleId.HasValue)
        {
            targetRoleId = await dbContext.CareerRoles
                .AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.Name)
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!targetRoleId.HasValue)
        {
            return BadRequest(new { message = "Không có định hướng nghề nghiệp nào đang hoạt động để liên kết với lộ trình." });
        }

        var title = string.IsNullOrWhiteSpace(request.Roadmap.Title)
            ? "Roadmap đề xuất bởi AI Mentor"
            : request.Roadmap.Title.Trim();

        var titleLower = title.ToLowerInvariant();
        var existingRoadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Title.ToLower() == titleLower, cancellationToken);

        if (existingRoadmap is not null)
        {
            var existingNodeCount = await dbContext.RoadmapNodes
                .CountAsync(n => n.RoadmapId == existingRoadmap.Id, cancellationToken);

            return Ok(new ApplyAiRoadmapResponse(
                existingRoadmap.Id,
                existingRoadmap.Title,
                existingNodeCount,
                true));
        }

        var now = DateTimeOffset.UtcNow;
        var roadmap = new Roadmap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareerRoleId = targetRoleId.Value,
            Title = title,
            Description = request.Roadmap.Description?.Trim(),
            Status = "Active",
            Progress = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Roadmaps.Add(roadmap);

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
            .Select(item => new
            {
                item.Id,
                item.SkillId,
                item.Title,
                item.Difficulty,
                item.StorageObjectName,
                item.LessonNumber
            })
            .ToListAsync(cancellationToken);
        var resourceIdsByTitle = resources
            .GroupBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var resourceIdsBySkill = resources
            .Where(item => item.SkillId is not null)
            .GroupBy(item => item.SkillId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group
                    .OrderBy(item => item.LessonNumber)
                    .ThenBy(item => DifficultyRank(item.Difficulty))
                    .ThenBy(item => item.StorageObjectName == null ? 0 : 1)
                    .ThenBy(item => item.Title)
                    .Select(item => item.Id)
                    .ToList());

        var allowedModuleTitles = skillIdsByTitle.Keys
            .Concat(resourceIdsByTitle.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sanitizedNodes = SanitizeRoadmapCategories(request.Roadmap.Nodes, categorySet, allowedModuleTitles);

        var nodes = new List<RoadmapNode>();
        var nodeResources = new List<RoadmapNodeResource>();
        var globalOrder = 0;
        FlattenNodes(
            sanitizedNodes,
            roadmap.Id,
            parentId: null,
            level: 0,
            ref globalOrder,
            now,
            nodes,
            nodeResources,
            skillIdsByTitle,
            resourceIdsByTitle,
            resourceIdsBySkill);

        if (nodes.Count == 0)
        {
            return BadRequest(new { message = "Lộ trình phải chứa ít nhất một module." });
        }

        try
        {
            dbContext.RoadmapNodes.AddRange(nodes);
            dbContext.RoadmapNodeResources.AddRange(nodeResources);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Failed to apply AI roadmap for user {UserId}. Inner: {Inner}", userId, inner);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Không tạo được roadmap từ gợi ý AI.",
                detail = inner,
                type = "DbUpdateException"
            });
        }

        return Ok(new ApplyAiRoadmapResponse(
            roadmap.Id,
            roadmap.Title,
            nodes.Count,
            false));
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

    private static int DifficultyRank(string? difficulty) => difficulty?.Trim().ToLowerInvariant() switch
    {
        "beginner" or "basic" or "cÆ¡ báº£n" => 0,
        "intermediate" or "trung cáº¥p" => 1,
        "advanced" or "nĂ¢ng cao" => 2,
        _ => 3
    };

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token người dùng không hợp lệ.");
    }
}

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
