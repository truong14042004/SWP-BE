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
    [HttpPost("apply-roadmap")]
    public async Task<ActionResult<ApplyAiRoadmapResponse>> ApplyRoadmap(
        ApplyAiRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Roadmap is null)
        {
            return BadRequest(new { message = "Roadmap payload is required." });
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
            return BadRequest(new { message = "No active career role available to attach the roadmap." });
        }

        var now = DateTimeOffset.UtcNow;
        var roadmap = new Roadmap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareerRoleId = targetRoleId.Value,
            Title = string.IsNullOrWhiteSpace(request.Roadmap.Title)
                ? "Roadmap đề xuất bởi AI Mentor"
                : request.Roadmap.Title.Trim(),
            Description = request.Roadmap.Description?.Trim(),
            Status = "Active",
            Progress = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Roadmaps.Add(roadmap);

        var nodes = new List<RoadmapNode>();
        FlattenNodes(request.Roadmap.Nodes, roadmap.Id, parentId: null, level: 0, orderOffset: 0, now, nodes);

        if (nodes.Count == 0)
        {
            return BadRequest(new { message = "Roadmap must contain at least one node." });
        }

        dbContext.RoadmapNodes.AddRange(nodes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApplyAiRoadmapResponse(
            roadmap.Id,
            roadmap.Title,
            nodes.Count));
    }

    private static int FlattenNodes(
        IReadOnlyList<AiRoadmapNodeDto>? source,
        Guid roadmapId,
        Guid? parentId,
        int level,
        int orderOffset,
        DateTimeOffset now,
        List<RoadmapNode> output)
    {
        if (source is null) return orderOffset;

        var index = 0;
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item.Title)) continue;

            var nodeType = string.IsNullOrWhiteSpace(item.NodeType)
                ? (item.Children?.Count > 0 ? "Group" : "Module")
                : item.NodeType.Trim();

            var node = new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmapId,
                ParentNodeId = parentId,
                Title = item.Title.Trim(),
                Description = item.Description?.Trim(),
                NodeType = nodeType,
                Status = "NotStarted",
                Level = level,
                OrderIndex = orderOffset + index,
                EstimatedHours = item.EstimatedHours.HasValue && item.EstimatedHours.Value > 0
                    ? item.EstimatedHours.Value
                    : null,
                Priority = item.Priority is >= 1 and <= 10 ? item.Priority.Value : 5,
                CreatedAt = now,
                UpdatedAt = now
            };
            output.Add(node);

            FlattenNodes(item.Children, roadmapId, node.Id, level + 1, 0, now, output);
            index++;
        }

        return orderOffset + index;
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
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
    int NodeCount);
