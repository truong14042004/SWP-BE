using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
public sealed class RoadmapController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost("api/roadmap/generate")]
    public async Task<ActionResult<RoadmapResponse>> Generate(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var careerRoleId = request.CareerRoleId
            ?? await dbContext.StudentProfiles
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.TargetRoleId)
                .SingleOrDefaultAsync(cancellationToken);

        if (careerRoleId is null)
        {
            return BadRequest(new { message = "Career role is required before generating a roadmap." });
        }

        var careerRole = await dbContext.CareerRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == careerRoleId && role.IsActive, cancellationToken);

        if (careerRole is null)
        {
            return NotFound(new { message = "Career role was not found." });
        }

        var skillGapReportId = request.SkillGapReportId
            ?? await dbContext.SkillGapReports
                .Where(report => report.UserId == userId && report.CareerRoleId == careerRoleId)
                .OrderByDescending(report => report.CreatedAt)
                .Select(report => (Guid?)report.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (request.SkillGapReportId is not null)
        {
            var ownsReport = await dbContext.SkillGapReports.AnyAsync(
                report => report.Id == request.SkillGapReportId
                    && report.UserId == userId
                    && report.CareerRoleId == careerRoleId,
                cancellationToken);
            if (!ownsReport)
            {
                return BadRequest(new { message = "Skill gap report does not belong to the current user and career role." });
            }
        }

        var roadmap = new Roadmap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareerRoleId = careerRole.Id,
            SkillGapReportId = skillGapReportId,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? $"{careerRole.Name} learning roadmap"
                : request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = "Active",
            Progress = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var nodeInputs = skillGapReportId is not null
            ? await GetNodesFromSkillGapAsync(skillGapReportId.Value, cancellationToken)
            : await GetNodesFromRoleRequirementsAsync(careerRole.Id, cancellationToken);

        if (nodeInputs.Count == 0)
        {
            nodeInputs = GetFallbackNodes(careerRole.Name);
        }

        var nodes = nodeInputs
            .Select((input, index) => new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmap.Id,
                SkillId = input.SkillId,
                LearningResourceId = input.LearningResourceId,
                Title = input.Title,
                Description = input.Description,
                NodeType = input.NodeType,
                Status = "NotStarted",
                OrderIndex = index + 1,
                EstimatedHours = input.EstimatedHours,
                Priority = input.Priority,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        for (var index = 1; index < nodes.Count; index++)
        {
            nodes[index].PrerequisiteNodeId = nodes[index - 1].Id;
        }

        dbContext.Roadmaps.Add(roadmap);
        dbContext.RoadmapNodes.AddRange(nodes);
        await dbContext.SaveChangesAsync(cancellationToken);

        var responseNodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Where(node => node.RoadmapId == roadmap.Id)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = roadmap.Id }, ToResponse(roadmap, careerRole.Name, responseNodes));
    }

    [HttpGet("api/roadmap")]
    public async Task<ActionResult<IReadOnlyList<RoadmapResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var roadmaps = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(roadmap => roadmap.CareerRole)
            .Where(roadmap => roadmap.UserId == userId)
            .OrderByDescending(roadmap => roadmap.CreatedAt)
            .ToListAsync(cancellationToken);

        var roadmapIds = roadmaps.Select(roadmap => roadmap.Id).ToArray();
        var nodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Where(node => roadmapIds.Contains(node.RoadmapId))
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        var nodesByRoadmap = nodes
            .GroupBy(node => node.RoadmapId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<RoadmapNode>)group.ToList());

        return Ok(roadmaps.Select(roadmap => ToResponse(
            roadmap,
            roadmap.CareerRole.Name,
            nodesByRoadmap.GetValueOrDefault(roadmap.Id) ?? [])).ToList());
    }

    [HttpGet("api/roadmap/{id:guid}")]
    public async Task<ActionResult<RoadmapResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(item => item.CareerRole)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (roadmap is null)
        {
            return NotFound(new { message = "Roadmap was not found." });
        }

        var nodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Where(node => node.RoadmapId == roadmap.Id)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        return Ok(ToResponse(roadmap, roadmap.CareerRole.Name, nodes));
    }

    [HttpPut("api/roadmap-node/{id:guid}/status")]
    public async Task<ActionResult<RoadmapNodeResponse>> UpdateNodeStatus(
        Guid id,
        UpdateRoadmapNodeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var allowedStatuses = new[] { "NotStarted", "InProgress", "Completed", "Verified", "NeedReview" };
        if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid roadmap node status." });
        }

        var userId = GetCurrentUserId();
        var node = await dbContext.RoadmapNodes
            .Include(item => item.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == id && item.Roadmap.UserId == userId, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Roadmap node was not found." });
        }

        var normalizedStatus = allowedStatuses.Single(status => status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (node.PrerequisiteNodeId is not null && normalizedStatus is not "NotStarted")
        {
            var prerequisiteStatus = await dbContext.RoadmapNodes
                .Where(item => item.Id == node.PrerequisiteNodeId && item.RoadmapId == node.RoadmapId)
                .Select(item => item.Status)
                .SingleOrDefaultAsync(cancellationToken);

            if (prerequisiteStatus is not ("Completed" or "Verified"))
            {
                return BadRequest(new
                {
                    message = "Prerequisite roadmap node must be completed before this node can be started.",
                    prerequisiteNodeId = node.PrerequisiteNodeId
                });
            }
        }

        node.Status = normalizedStatus;
        node.UpdatedAt = DateTimeOffset.UtcNow;

        var roadmapNodes = await dbContext.RoadmapNodes
            .Where(item => item.RoadmapId == node.RoadmapId)
            .ToListAsync(cancellationToken);
        var completedCount = roadmapNodes.Count(item => item.Id == node.Id
            ? node.Status is "Completed" or "Verified"
            : item.Status is "Completed" or "Verified");

        node.Roadmap.Progress = roadmapNodes.Count == 0
            ? 0
            : Math.Round(completedCount * 100m / roadmapNodes.Count, 2);
        node.Roadmap.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToNodeResponse(node));
    }

    private async Task<List<RoadmapNodeInput>> GetNodesFromSkillGapAsync(
        Guid skillGapReportId,
        CancellationToken cancellationToken)
    {
        return await dbContext.SkillGapReportItems
            .AsNoTracking()
            .Where(item => item.SkillGapReportId == skillGapReportId)
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.Skill.Name)
            .Select(item => new RoadmapNodeInput(
                item.SkillId,
                dbContext.LearningResources
                    .Where(resource => resource.SkillId == item.SkillId && resource.IsActive)
                    .OrderBy(resource => resource.Difficulty)
                    .Select(resource => (Guid?)resource.Id)
                    .FirstOrDefault(),
                $"Improve {item.Skill.Name}",
                item.Recommendation ?? $"Reach {item.RequiredLevel} level for {item.Skill.Name}.",
                "Skill",
                item.Priority,
                item.Priority <= 2 ? 12 : 8))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<RoadmapNodeInput>> GetNodesFromRoleRequirementsAsync(
        Guid careerRoleId,
        CancellationToken cancellationToken)
    {
        var userSkills = await dbContext.UserSkills
            .AsNoTracking()
            .Where(userSkill => userSkill.UserId == GetCurrentUserId())
            .ToDictionaryAsync(userSkill => userSkill.SkillId, cancellationToken);

        var requirements = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Include(requirement => requirement.Skill)
            .Where(requirement => requirement.CareerRoleId == careerRoleId)
            .OrderBy(requirement => requirement.Priority)
            .ThenBy(requirement => requirement.Skill.Name)
            .ToListAsync(cancellationToken);

        var resources = await dbContext.LearningResources
            .AsNoTracking()
            .Where(resource => resource.SkillId != null && resource.IsActive)
            .GroupBy(resource => resource.SkillId!.Value)
            .Select(group => new
            {
                SkillId = group.Key,
                ResourceId = group.OrderBy(resource => resource.Difficulty).Select(resource => resource.Id).First()
            })
            .ToDictionaryAsync(item => item.SkillId, item => (Guid?)item.ResourceId, cancellationToken);

        return requirements
            .Where(requirement => !userSkills.TryGetValue(requirement.SkillId, out var userSkill)
                || LevelRank(userSkill.Level) < LevelRank(requirement.RequiredLevel)
                || !userSkill.IsVerified)
            .Select(requirement =>
            {
                var hasSkill = userSkills.TryGetValue(requirement.SkillId, out var userSkill);
                var priority = hasSkill
                    ? Math.Min(requirement.Priority + 1, 5)
                    : requirement.Priority;
                var status = hasSkill && !userSkill!.IsVerified ? "Needs evidence/verification." : "Missing or weak skill.";

                return new RoadmapNodeInput(
                    requirement.SkillId,
                    resources.GetValueOrDefault(requirement.SkillId),
                    $"Learn {requirement.Skill.Name}",
                    $"{status} Reach {requirement.RequiredLevel} level for {requirement.Skill.Name}.",
                    "Skill",
                    priority,
                    priority <= 2 ? 12 : 8);
            })
            .ToList();
    }

    private static List<RoadmapNodeInput> GetFallbackNodes(string careerRoleName)
    {
        var normalizedRole = careerRoleName.ToLowerInvariant();
        var common = new List<RoadmapNodeInput>
        {
            new(null, null, "Clarify target role expectations", $"Review internship/fresher requirements for {careerRoleName} and list the top missing skills.", "Reading", 1, 4),
            new(null, null, "Build one portfolio project", $"Create a practical project aligned with {careerRoleName} and document the problem, features, tech stack, and setup steps.", "Project", 2, 24),
            new(null, null, "Improve GitHub presentation", "Add README, screenshots, setup guide, API examples, deployment link, and architecture notes.", "Practice", 3, 6)
        };

        if (normalizedRole.Contains("backend"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Master REST API fundamentals", "Design CRUD APIs with validation, authentication, pagination, and clear error responses.", "Skill", 1, 16),
                new(null, null, "Practice database design", "Model entities, relationships, indexes, migrations, and query patterns for a real backend feature.", "Skill", 2, 14),
                new(null, null, "Add backend testing", "Write unit and integration tests for service logic, controllers, and database behavior.", "Practice", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("frontend"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Strengthen React UI architecture", "Build reusable components, route protection, form validation, loading states, and error states.", "Skill", 1, 16),
                new(null, null, "Integrate real APIs", "Connect frontend pages to authenticated backend APIs with clean API client handling.", "Practice", 2, 12),
                new(null, null, "Polish responsive UX", "Verify mobile and desktop layouts, accessibility, and empty/error states.", "Assessment", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("devops") || normalizedRole.Contains("cloud"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Containerize the application", "Write Dockerfile and compose setup for backend, frontend, and database.", "Project", 1, 12),
                new(null, null, "Build CI/CD pipeline", "Automate build, test, migration, and deployment steps.", "Practice", 2, 14),
                new(null, null, "Add monitoring basics", "Track logs, health checks, deployment status, and rollback process.", "Skill", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("data"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Practice SQL and data modeling", "Design analytical schemas, transformations, and query optimization examples.", "Skill", 1, 16),
                new(null, null, "Build a data pipeline", "Ingest, clean, transform, and export a dataset with reproducible scripts.", "Project", 2, 20),
                new(null, null, "Document data quality checks", "Add validation rules, data profiling, and pipeline failure handling.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("qa"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Write test strategy", "Define test scope, acceptance criteria, and regression risks for a sample product.", "Reading", 1, 8),
                new(null, null, "Automate API and UI tests", "Create repeatable tests for main user flows and API contracts.", "Project", 2, 18),
                new(null, null, "Set up test reporting", "Publish test results and document bugs with reproduction steps.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("ai"))
        {
            common.InsertRange(1,
            [
                new(null, null, "Learn AI API integration", "Build prompts, structured outputs, retries, and error handling around an AI provider.", "Skill", 1, 14),
                new(null, null, "Build an AI-assisted feature", "Create a feature that uses user context and stores generation results.", "Project", 2, 20),
                new(null, null, "Evaluate AI output quality", "Add validation, safety checks, and feedback capture for generated content.", "Assessment", 3, 10)
            ]);
        }

        return common
            .Select((node, index) => node with { Priority = Math.Clamp(node.Priority, 1, 5) })
            .ToList();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private static int LevelRank(string? level) =>
        level?.Trim().ToLowerInvariant() switch
        {
            "verified" => 4,
            "advanced" => 3,
            "intermediate" => 2,
            "beginner" => 1,
            _ => 0
        };

    private static RoadmapResponse ToResponse(Roadmap roadmap, string careerRoleName, IReadOnlyList<RoadmapNode> nodes) =>
        new(
            roadmap.Id,
            roadmap.CareerRoleId,
            careerRoleName,
            roadmap.SkillGapReportId,
            roadmap.Title,
            roadmap.Description,
            roadmap.Status,
            roadmap.Progress,
            roadmap.CreatedAt,
            roadmap.UpdatedAt,
            nodes.OrderBy(node => node.OrderIndex).Select(ToNodeResponse).ToList());

    private static RoadmapNodeResponse ToNodeResponse(RoadmapNode node) =>
        new(
            node.Id,
            node.SkillId,
            node.LearningResourceId,
            node.PrerequisiteNodeId,
            node.Title,
            node.Description,
            node.NodeType,
            node.Status,
            node.OrderIndex,
            node.EstimatedHours,
            node.Priority,
            node.LearningResource is null ? null : ToLearningResourceResponse(node.LearningResource));

    private static RoadmapLearningResourceResponse ToLearningResourceResponse(LearningResource resource) =>
        new(
            resource.Id,
            resource.SkillId,
            resource.Skill?.Name,
            resource.Title,
            resource.Url,
            resource.StorageObjectName is null ? "Link" : "File",
            resource.ContentType,
            resource.FileSize,
            resource.ResourceType,
            resource.Difficulty,
            resource.EstimatedHours);

    private sealed record RoadmapNodeInput(
        Guid? SkillId,
        Guid? LearningResourceId,
        string Title,
        string Description,
        string NodeType,
        int Priority,
        int EstimatedHours);
}

public sealed record GenerateRoadmapRequest(
    Guid? CareerRoleId,
    Guid? SkillGapReportId,
    string? Title,
    string? Description);

public sealed record UpdateRoadmapNodeStatusRequest(string Status);

public sealed record RoadmapResponse(
    Guid Id,
    Guid CareerRoleId,
    string CareerRoleName,
    Guid? SkillGapReportId,
    string Title,
    string? Description,
    string Status,
    decimal Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RoadmapNodeResponse> Nodes);

public sealed record RoadmapNodeResponse(
    Guid Id,
    Guid? SkillId,
    Guid? LearningResourceId,
    Guid? PrerequisiteNodeId,
    string Title,
    string? Description,
    string NodeType,
    string Status,
    int OrderIndex,
    int? EstimatedHours,
    int Priority,
    RoadmapLearningResourceResponse? LearningResource);

public sealed record RoadmapLearningResourceResponse(
    Guid Id,
    Guid? SkillId,
    string? SkillName,
    string Title,
    string Url,
    string SourceType,
    string? ContentType,
    long? FileSize,
    string ResourceType,
    string? Difficulty,
    int? EstimatedHours);
