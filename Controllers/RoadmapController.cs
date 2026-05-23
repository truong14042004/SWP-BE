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
            return BadRequest(new { message = "Cần chọn định hướng nghề nghiệp trước khi tạo lộ trình." });
        }

        var careerRole = await dbContext.CareerRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == careerRoleId && role.IsActive, cancellationToken);

        if (careerRole is null)
        {
            return NotFound(new { message = "Không tìm thấy định hướng nghề nghiệp." });
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
                return BadRequest(new { message = "Bản báo cáo khoảng cách kỹ năng không thuộc về người dùng hoặc định hướng nghề nghiệp hiện tại." });
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

        var hierarchy = BuildHierarchyNodes(roadmap.Id, nodeInputs, now);

        var nodeResources = hierarchy.ActionNodes
            .SelectMany(pair => pair.Input.LearningResourceIds
                .Distinct()
                .Select((resourceId, resourceIndex) => new RoadmapNodeResource
                {
                    Id = Guid.NewGuid(),
                    RoadmapNodeId = pair.Node.Id,
                    LearningResourceId = resourceId,
                    OrderIndex = resourceIndex + 1,
                    CreatedAt = now
                }))
            .ToList();

        dbContext.Roadmaps.Add(roadmap);
        dbContext.RoadmapNodes.AddRange(hierarchy.Nodes);
        dbContext.RoadmapNodeResources.AddRange(nodeResources);
        await dbContext.SaveChangesAsync(cancellationToken);

        var responseNodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(node => node.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
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
            .Include(node => node.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
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
            return NotFound(new { message = "Không tìm thấy lộ trình học tập." });
        }

        var nodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(node => node.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
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
        var allowedStatuses = new[] { "NotStarted", "InProgress", "Completed", "NeedReview" };
        if (!allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Trạng thái không hợp lệ. Vui lòng sử dụng endpoint verify để đổi sang Verified."
            });
        }

        var userId = GetCurrentUserId();
        var node = await dbContext.RoadmapNodes
            .Include(item => item.Roadmap)
            .Include(item => item.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(item => item.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
            .SingleOrDefaultAsync(item => item.Id == id && item.Roadmap.UserId == userId, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Không tìm thấy module lộ trình." });
        }

        if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Nhóm module là thư mục chứa lộ trình. Vui lòng cập nhật các module kỹ thuật con bên trong." });
        }

        if (node.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Các module đã được xác minh chỉ có thể thay đổi bởi mentor hoặc cố vấn."
            });
        }

        var normalizedStatus = allowedStatuses.Single(status => status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (node.PrerequisiteNodeId is not null && normalizedStatus is not "NotStarted")
        {
            var prerequisiteStatus = await dbContext.RoadmapNodes
                .Where(item => item.Id == node.PrerequisiteNodeId && item.RoadmapId == node.RoadmapId)
                .Select(item => item.Status)
                .SingleOrDefaultAsync(cancellationToken);

            if (prerequisiteStatus is not ("Completed" or "Verified" or "NeedReview"))
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

        await RecalculateRoadmapProgressAsync(node, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToNodeResponse(node));
    }

    [Authorize(Roles = $"{UserRoles.AcademicCounselor},{UserRoles.IndustryMentor},{UserRoles.Admin}")]
    [HttpPut("api/roadmap-node/{id:guid}/verify")]
    public async Task<ActionResult<RoadmapNodeResponse>> VerifyNode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var node = await dbContext.RoadmapNodes
            .Include(item => item.Roadmap)
            .Include(item => item.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(item => item.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Không tìm thấy module lộ trình." });
        }

        if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Không thể trực tiếp xác minh nhóm module." });
        }

        if (!(node.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
              node.Status.Equals("NeedReview", StringComparison.OrdinalIgnoreCase) ||
              node.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "Chỉ các module được đánh dấu Hoàn thành (Completed) hoặc Cần đánh giá (NeedReview) mới có thể xác minh."
            });
        }

        node.Status = "Verified";
        node.UpdatedAt = DateTimeOffset.UtcNow;

        await RecalculateRoadmapProgressAsync(node, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToNodeResponse(node));
    }

    private async Task RecalculateRoadmapProgressAsync(RoadmapNode node, CancellationToken cancellationToken)
    {
        var roadmapNodes = await dbContext.RoadmapNodes
            .Where(item => item.RoadmapId == node.RoadmapId)
            .ToListAsync(cancellationToken);
        var progressNodes = roadmapNodes
            .Where(item => !item.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var completedCount = progressNodes.Count(item => item.Id == node.Id
            ? node.Status is "Completed" or "Verified"
            : item.Status is "Completed" or "Verified");

        node.Roadmap.Progress = progressNodes.Count == 0
            ? 0
            : Math.Round(completedCount * 100m / progressNodes.Count, 2);
        node.Roadmap.UpdatedAt = DateTimeOffset.UtcNow;
    }


    private static RoadmapHierarchy BuildHierarchyNodes(
        Guid roadmapId,
        IReadOnlyList<RoadmapNodeInput> nodeInputs,
        DateTimeOffset now)
    {
        var nodes = new List<RoadmapNode>();
        var actionNodes = new List<RoadmapActionNode>();
        Guid? previousActionNodeId = null;
        var orderIndex = 1;

        var groupedInputs = nodeInputs
            .Select((input, index) => new { Input = input, Index = index })
            .GroupBy(item => NormalizeGroupName(item.Input.GroupName))
            .OrderBy(group => group.Min(item => item.Input.Priority))
            .ThenBy(group => group.Min(item => item.Index));

        foreach (var group in groupedInputs)
        {
            var orderedChildren = group
                .OrderBy(item => item.Input.Priority)
                .ThenBy(item => item.Index)
                .ToList();
            var groupPriority = Math.Clamp(orderedChildren.Min(item => item.Input.Priority), 1, 5);
            var groupNode = new RoadmapNode
            {
                Id = Guid.NewGuid(),
                RoadmapId = roadmapId,
                Title = BuildGroupTitle(group.Key),
                Description = BuildGroupDescription(group.Key),
                NodeType = "Group",
                Status = "NotStarted",
                Level = 0,
                OrderIndex = orderIndex++,
                EstimatedHours = orderedChildren.Sum(item => item.Input.EstimatedHours),
                Priority = groupPriority,
                CreatedAt = now,
                UpdatedAt = now
            };

            nodes.Add(groupNode);

            foreach (var item in orderedChildren)
            {
                var input = item.Input;
                var node = new RoadmapNode
                {
                    Id = Guid.NewGuid(),
                    RoadmapId = roadmapId,
                    SkillId = input.SkillId,
                    LearningResourceId = input.LearningResourceIds.Select(resourceId => (Guid?)resourceId).FirstOrDefault(),
                    ParentNodeId = groupNode.Id,
                    PrerequisiteNodeId = previousActionNodeId,
                    Title = input.Title,
                    Description = input.Description,
                    NodeType = input.NodeType,
                    Status = "NotStarted",
                    Level = 1,
                    OrderIndex = orderIndex++,
                    EstimatedHours = input.EstimatedHours,
                    Priority = Math.Clamp(input.Priority, 1, 5),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                nodes.Add(node);
                actionNodes.Add(new RoadmapActionNode(node, input));
                previousActionNodeId = node.Id;
            }
        }

        return new RoadmapHierarchy(nodes, actionNodes);
    }

    private async Task<List<RoadmapNodeInput>> GetNodesFromSkillGapAsync(
        Guid skillGapReportId,
        CancellationToken cancellationToken)
    {
        var reportItems = await dbContext.SkillGapReportItems
            .AsNoTracking()
            .Include(item => item.Skill)
            .Where(item => item.SkillGapReportId == skillGapReportId)
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.Skill.Name)
            .ToListAsync(cancellationToken);

        var skillIds = reportItems.Select(item => item.SkillId).Distinct().ToArray();
        var resourcesBySkill = await GetActiveResourcesBySkillAsync(skillIds, cancellationToken);

        return reportItems
            .Select(item => new RoadmapNodeInput(
                item.SkillId,
                resourcesBySkill.GetValueOrDefault(item.SkillId) ?? [],
                BuildGroupName(item.Skill.Category, item.Priority),
                $"Improve {item.Skill.Name}",
                item.Recommendation ?? $"Reach {item.RequiredLevel} level for {item.Skill.Name}.",
                "Skill",
                item.Priority,
                item.Priority <= 2 ? 12 : 8))
            .ToList();
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

        var requirementSkillIds = requirements.Select(requirement => requirement.SkillId).Distinct().ToArray();
        var resourcesBySkill = await GetActiveResourcesBySkillAsync(requirementSkillIds, cancellationToken);

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
                    resourcesBySkill.GetValueOrDefault(requirement.SkillId) ?? [],
                    BuildGroupName(requirement.Skill.Category, priority),
                    $"Learn {requirement.Skill.Name}",
                    $"{status} Reach {requirement.RequiredLevel} level for {requirement.Skill.Name}.",
                    "Skill",
                    priority,
                    priority <= 2 ? 12 : 8);
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<Guid>>> GetActiveResourcesBySkillAsync(
        IReadOnlyCollection<Guid> skillIds,
        CancellationToken cancellationToken)
    {
        if (skillIds.Count == 0)
        {
            return [];
        }

        var resources = await dbContext.LearningResources
            .AsNoTracking()
            .Where(resource => resource.SkillId != null
                && skillIds.Contains(resource.SkillId.Value)
                && resource.IsActive)
            .Select(resource => new
            {
                SkillId = resource.SkillId!.Value,
                resource.Id,
                resource.Difficulty,
                resource.StorageObjectName,
                resource.Title
            })
            .ToListAsync(cancellationToken);

        return resources
            .GroupBy(resource => resource.SkillId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group
                    .OrderBy(resource => DifficultyRank(resource.Difficulty))
                    .ThenBy(resource => resource.StorageObjectName == null ? 0 : 1)
                    .ThenBy(resource => resource.Title)
                    .Select(resource => resource.Id)
                    .ToList());
    }

    private static List<RoadmapNodeInput> GetFallbackNodes(string careerRoleName)
    {
        var normalizedRole = careerRoleName.ToLowerInvariant();
        var common = new List<RoadmapNodeInput>
        {
            new(null, [], "Foundation", "Clarify target role expectations", $"Review internship/fresher requirements for {careerRoleName} and list the top missing skills.", "Reading", 1, 4),
            new(null, [], "Portfolio", "Build one portfolio project", $"Create a practical project aligned with {careerRoleName} and document the problem, features, tech stack, and setup steps.", "Project", 2, 24),
            new(null, [], "Portfolio", "Improve GitHub presentation", "Add README, screenshots, setup guide, API examples, deployment link, and architecture notes.", "Practice", 3, 6)
        };

        if (normalizedRole.Contains("backend"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Backend Engineering", "Master REST API fundamentals", "Design CRUD APIs with validation, authentication, pagination, and clear error responses.", "Skill", 1, 16),
                new(null, [], "Backend Engineering", "Practice database design", "Model entities, relationships, indexes, migrations, and query patterns for a real backend feature.", "Skill", 2, 14),
                new(null, [], "Quality", "Add backend testing", "Write unit and integration tests for service logic, controllers, and database behavior.", "Practice", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("frontend"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Frontend Engineering", "Strengthen React UI architecture", "Build reusable components, route protection, form validation, loading states, and error states.", "Skill", 1, 16),
                new(null, [], "Frontend Engineering", "Integrate real APIs", "Connect frontend pages to authenticated backend APIs with clean API client handling.", "Practice", 2, 12),
                new(null, [], "Quality", "Polish responsive UX", "Verify mobile and desktop layouts, accessibility, and empty/error states.", "Assessment", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("devops") || normalizedRole.Contains("cloud"))
        {
            common.InsertRange(1,
            [
                new(null, [], "DevOps Foundation", "Containerize the application", "Write Dockerfile and compose setup for backend, frontend, and database.", "Project", 1, 12),
                new(null, [], "DevOps Foundation", "Build CI/CD pipeline", "Automate build, test, migration, and deployment steps.", "Practice", 2, 14),
                new(null, [], "Operations", "Add monitoring basics", "Track logs, health checks, deployment status, and rollback process.", "Skill", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("data"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Data Foundation", "Practice SQL and data modeling", "Design analytical schemas, transformations, and query optimization examples.", "Skill", 1, 16),
                new(null, [], "Data Engineering", "Build a data pipeline", "Ingest, clean, transform, and export a dataset with reproducible scripts.", "Project", 2, 20),
                new(null, [], "Quality", "Document data quality checks", "Add validation rules, data profiling, and pipeline failure handling.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("qa"))
        {
            common.InsertRange(1,
            [
                new(null, [], "QA Foundation", "Write test strategy", "Define test scope, acceptance criteria, and regression risks for a sample product.", "Reading", 1, 8),
                new(null, [], "QA Automation", "Automate API and UI tests", "Create repeatable tests for main user flows and API contracts.", "Project", 2, 18),
                new(null, [], "Quality", "Set up test reporting", "Publish test results and document bugs with reproduction steps.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("ai"))
        {
            common.InsertRange(1,
            [
                new(null, [], "AI Engineering", "Learn AI API integration", "Build prompts, structured outputs, retries, and error handling around an AI provider.", "Skill", 1, 14),
                new(null, [], "AI Engineering", "Build an AI-assisted feature", "Create a feature that uses user context and stores generation results.", "Project", 2, 20),
                new(null, [], "Quality", "Evaluate AI output quality", "Add validation, safety checks, and feedback capture for generated content.", "Assessment", 3, 10)
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

    private static int DifficultyRank(string? difficulty) =>
        difficulty?.Trim().ToLowerInvariant() switch
        {
            "beginner" or "basic" or "fundamental" or "fundamentals" => 1,
            "intermediate" => 2,
            "advanced" => 3,
            "expert" => 4,
            _ => 5
        };

    private static string BuildGroupName(string? category, int priority)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return category.Trim();
        }

        return priority switch
        {
            <= 1 => "Foundation",
            2 => "Core Skills",
            3 => "Applied Practice",
            _ => "Portfolio"
        };
    }

    private static string NormalizeGroupName(string? groupName) =>
        string.IsNullOrWhiteSpace(groupName) ? "General" : groupName.Trim();

    private static string BuildGroupTitle(string groupName) =>
        groupName.Equals("General", StringComparison.OrdinalIgnoreCase)
            ? "General Roadmap"
            : groupName;

    private static string BuildGroupDescription(string groupName) =>
        $"Complete the {groupName.ToLowerInvariant()} nodes in priority order before moving to the next learning area.";

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
            nodes.OrderBy(node => node.OrderIndex).Select(node => ToNodeResponse(node)).ToList(),
            BuildNodeTree(nodes));

    private static IReadOnlyList<RoadmapNodeResponse> BuildNodeTree(IReadOnlyList<RoadmapNode> nodes)
    {
        var nodesByParent = nodes
            .OrderBy(node => node.OrderIndex)
            .GroupBy(node => node.ParentNodeId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        return BuildNodeChildren(Guid.Empty, nodesByParent);
    }

    private static IReadOnlyList<RoadmapNodeResponse> BuildNodeChildren(
        Guid parentNodeId,
        IReadOnlyDictionary<Guid, List<RoadmapNode>> nodesByParent)
    {
        if (!nodesByParent.TryGetValue(parentNodeId, out var children))
        {
            return [];
        }

        return children
            .OrderBy(node => node.OrderIndex)
            .Select(node => ToNodeResponse(node, BuildNodeChildren(node.Id, nodesByParent)))
            .ToList();
    }

    private static RoadmapNodeResponse ToNodeResponse(
        RoadmapNode node,
        IReadOnlyList<RoadmapNodeResponse>? children = null)
    {
        var learningResources = node.Resources
            .OrderBy(item => item.OrderIndex)
            .Select(item => ToLearningResourceResponse(item.LearningResource))
            .ToList();
        var primaryLearningResource = learningResources.FirstOrDefault()
            ?? (node.LearningResource is null ? null : ToLearningResourceResponse(node.LearningResource));

        return new(
            node.Id,
            node.SkillId,
            node.LearningResourceId,
            node.PrerequisiteNodeId,
            node.ParentNodeId,
            node.Title,
            node.Description,
            node.NodeType,
            node.Status,
            node.Level,
            node.OrderIndex,
            node.EstimatedHours,
            node.Priority,
            primaryLearningResource,
            learningResources,
            children ?? []);
    }

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
        IReadOnlyList<Guid> LearningResourceIds,
        string GroupName,
        string Title,
        string Description,
        string NodeType,
        int Priority,
        int EstimatedHours);

    private sealed record RoadmapActionNode(RoadmapNode Node, RoadmapNodeInput Input);

    private sealed record RoadmapHierarchy(
        IReadOnlyList<RoadmapNode> Nodes,
        IReadOnlyList<RoadmapActionNode> ActionNodes);
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
    IReadOnlyList<RoadmapNodeResponse> Nodes,
    IReadOnlyList<RoadmapNodeResponse> NodeTree);

public sealed record RoadmapNodeResponse(
    Guid Id,
    Guid? SkillId,
    Guid? LearningResourceId,
    Guid? PrerequisiteNodeId,
    Guid? ParentNodeId,
    string Title,
    string? Description,
    string NodeType,
    string Status,
    int Level,
    int OrderIndex,
    int? EstimatedHours,
    int Priority,
    RoadmapLearningResourceResponse? LearningResource,
    IReadOnlyList<RoadmapLearningResourceResponse> LearningResources,
    IReadOnlyList<RoadmapNodeResponse> Children);

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
