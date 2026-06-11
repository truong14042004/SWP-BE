using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
public sealed class RoadmapController(
    AppDbContext dbContext,
    IUserSkillSyncService userSkillSyncService,
    INotificationService notificationService) : ControllerBase
{
    [HttpPost("api/roadmap/generate")]
    public async Task<ActionResult<RoadmapResponse>> Generate(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var pendingVerificationCount = await dbContext.UserSkills
            .CountAsync(
                userSkill => userSkill.UserId == userId
                    && userSkill.VerificationStatus == UserSkillVerificationStatus.PendingVerification,
                cancellationToken);
        if (pendingVerificationCount > 0)
        {
            return BadRequest(new
            {
                message = $"Bạn có {pendingVerificationCount} kỹ năng đang chờ xác thực. Hãy đợi cố vấn duyệt hoặc chuyển sang chưa xác thực trước khi tạo lộ trình."
            });
        }

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
                ? $"Lộ trình học tập {careerRole.Name}"
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

        var prerequisiteSkillIds = nodeInputs
            .Where(input => input.SkillId is not null)
            .Select(input => input.SkillId!.Value)
            .Distinct()
            .ToArray();
        var prerequisiteMap = await GetSkillPrerequisiteMapAsync(prerequisiteSkillIds, cancellationToken);
        nodeInputs = TopologicalOrderInputs(nodeInputs, prerequisiteMap);

        var hierarchy = BuildHierarchyNodes(roadmap.Id, nodeInputs, prerequisiteMap, now);

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

        return CreatedAtAction(nameof(GetById), new { id = roadmap.Id }, ToResponse(roadmap, careerRole.Name, responseNodes, false));
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

        var careerRoleIds = roadmaps.Select(r => r.CareerRoleId).Distinct().ToList();

        var requirementsLatestUpdates = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(req => careerRoleIds.Contains(req.CareerRoleId))
            .GroupBy(req => req.CareerRoleId)
            .Select(g => new { CareerRoleId = g.Key, LatestUpdate = g.Max(req => req.UpdatedAt) })
            .ToDictionaryAsync(item => item.CareerRoleId, item => item.LatestUpdate, cancellationToken);

        var rolesLatestUpdates = await dbContext.CareerRoles
            .AsNoTracking()
            .Where(role => careerRoleIds.Contains(role.Id))
            .ToDictionaryAsync(role => role.Id, role => role.UpdatedAt, cancellationToken);

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

        var responseList = roadmaps.Select(roadmap => {
            var reqUpdate = requirementsLatestUpdates.GetValueOrDefault(roadmap.CareerRoleId, DateTimeOffset.MinValue);
            var roleUpdate = rolesLatestUpdates.GetValueOrDefault(roadmap.CareerRoleId, DateTimeOffset.MinValue);
            var latestUpdate = reqUpdate > roleUpdate ? reqUpdate : roleUpdate;
            bool isOutdated = roadmap.CreatedAt < latestUpdate;

            return ToResponse(
                roadmap,
                roadmap.CareerRole.Name,
                nodesByRoadmap.GetValueOrDefault(roadmap.Id) ?? [],
                isOutdated);
        }).ToList();

        return Ok(responseList);
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

        var reqUpdate = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(req => req.CareerRoleId == roadmap.CareerRoleId)
            .Select(req => (DateTimeOffset?)req.UpdatedAt)
            .MaxAsync(cancellationToken) ?? DateTimeOffset.MinValue;

        var roleUpdate = roadmap.CareerRole.UpdatedAt;
        var latestUpdate = reqUpdate > roleUpdate ? reqUpdate : roleUpdate;
        bool isOutdated = roadmap.CreatedAt < latestUpdate;

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

        return Ok(ToResponse(roadmap, roadmap.CareerRole.Name, nodes, isOutdated));
    }

    [HttpPost("api/roadmap/{id:guid}/regenerate")]
    public async Task<ActionResult<RoadmapResponse>> Regenerate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        var existingRoadmap = await dbContext.Roadmaps
            .Include(r => r.CareerRole)
            .SingleOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

        if (existingRoadmap is null)
        {
            return NotFound(new { message = "Không tìm thấy lộ trình học tập để cập nhật." });
        }

        var careerRoleId = existingRoadmap.CareerRoleId;
        var careerRole = existingRoadmap.CareerRole;

        var skillGapReportId = existingRoadmap.SkillGapReportId
            ?? await dbContext.SkillGapReports
                .Where(report => report.UserId == userId && report.CareerRoleId == careerRoleId)
                .OrderByDescending(report => report.CreatedAt)
                .Select(report => (Guid?)report.Id)
                .FirstOrDefaultAsync(cancellationToken);

        // Load existing nodes to preserve completed/verified status
        var existingNodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Where(node => node.RoadmapId == id)
            .ToListAsync(cancellationToken);

        var existingSkillStatus = existingNodes
            .Where(n => n.SkillId is not null)
            .GroupBy(n => n.SkillId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Status);

        var existingTitleStatus = existingNodes
            .Where(n => n.SkillId is null)
            .GroupBy(n => n.Title)
            .ToDictionary(g => g.Key, g => g.First().Status);

        var nodeInputs = skillGapReportId is not null
            ? await GetNodesFromSkillGapAsync(skillGapReportId.Value, cancellationToken)
            : await GetNodesFromRoleRequirementsAsync(careerRole.Id, cancellationToken);

        if (nodeInputs.Count == 0)
        {
            nodeInputs = GetFallbackNodes(careerRole.Name);
        }

        var prerequisiteSkillIds = nodeInputs
            .Where(input => input.SkillId is not null)
            .Select(input => input.SkillId!.Value)
            .Distinct()
            .ToArray();
        var prerequisiteMap = await GetSkillPrerequisiteMapAsync(prerequisiteSkillIds, cancellationToken);
        nodeInputs = TopologicalOrderInputs(nodeInputs, prerequisiteMap);

        // Load and keep review requests & lesson progress
        var oldNodeIds = existingNodes.Select(n => n.Id).ToList();
        var reviewRequests = await dbContext.RoadmapNodeReviewRequests
            .Where(r => oldNodeIds.Contains(r.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        var lessonProgresses = await dbContext.LessonProgresses
            .Where(lp => oldNodeIds.Contains(lp.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        // Create new hierarchy
        var hierarchy = BuildHierarchyNodes(id, nodeInputs, prerequisiteMap, now);

        // Map matching old node IDs to new node IDs
        var newNodeMap = new Dictionary<string, Guid>();
        foreach (var node in hierarchy.Nodes)
        {
            if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase)) continue;
            if (node.SkillId is not null)
            {
                newNodeMap[$"skill_{node.SkillId.Value}"] = node.Id;
            }
            else
            {
                newNodeMap[$"title_{node.Title}"] = node.Id;
            }
        }

        // Restore status for matching nodes in the new hierarchy
        foreach (var node in hierarchy.Nodes)
        {
            if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase)) continue;

            if (node.SkillId is not null && existingSkillStatus.TryGetValue(node.SkillId.Value, out var skillStatus))
            {
                node.Status = skillStatus;
            }
            else if (node.SkillId is null && existingTitleStatus.TryGetValue(node.Title, out var titleStatus))
            {
                node.Status = titleStatus;
            }
        }

        // Redirect review requests
        var requestsToDelete = new List<RoadmapNodeReviewRequest>();
        foreach (var request in reviewRequests)
        {
            var oldNode = existingNodes.FirstOrDefault(n => n.Id == request.RoadmapNodeId);
            if (oldNode is null)
            {
                requestsToDelete.Add(request);
                continue;
            }

            string key = oldNode.SkillId is not null ? $"skill_{oldNode.SkillId.Value}" : $"title_{oldNode.Title}";
            if (newNodeMap.TryGetValue(key, out var newNodeId))
            {
                request.RoadmapNodeId = newNodeId;
            }
            else
            {
                requestsToDelete.Add(request);
            }
        }

        // Redirect lesson progress
        var progressToDelete = new List<LessonProgress>();
        var seenProgress = new HashSet<(Guid UserId, Guid NodeId, Guid ResourceId)>();
        foreach (var progressItem in lessonProgresses)
        {
            var oldNode = existingNodes.FirstOrDefault(n => n.Id == progressItem.RoadmapNodeId);
            if (oldNode is null)
            {
                progressToDelete.Add(progressItem);
                continue;
            }

            string key = oldNode.SkillId is not null ? $"skill_{oldNode.SkillId.Value}" : $"title_{oldNode.Title}";
            if (newNodeMap.TryGetValue(key, out var newNodeId))
            {
                var compositeKey = (progressItem.UserId, newNodeId, progressItem.LearningResourceId);
                if (seenProgress.Contains(compositeKey))
                {
                    progressToDelete.Add(progressItem);
                }
                else
                {
                    progressItem.RoadmapNodeId = newNodeId;
                    seenProgress.Add(compositeKey);
                }
            }
            else
            {
                progressToDelete.Add(progressItem);
            }
        }

        // Delete old resources and nodes
        var oldNodeResources = await dbContext.RoadmapNodeResources
            .Where(r => oldNodeIds.Contains(r.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        dbContext.RoadmapNodeResources.RemoveRange(oldNodeResources);
        dbContext.RoadmapNodes.RemoveRange(existingNodes);

        dbContext.RoadmapNodeReviewRequests.RemoveRange(requestsToDelete);
        dbContext.LessonProgresses.RemoveRange(progressToDelete);

        // Add new nodes and their resources
        dbContext.RoadmapNodes.AddRange(hierarchy.Nodes);

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
        dbContext.RoadmapNodeResources.AddRange(nodeResources);

        // Update existing roadmap properties
        existingRoadmap.UpdatedAt = now;
        existingRoadmap.SkillGapReportId = skillGapReportId;

        // Recalculate progress based on restored node statuses
        var progressNodes = hierarchy.Nodes
            .Where(item => !item.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var completedCount = progressNodes.Count(item => item.Status is "Completed" or "Verified");
        existingRoadmap.Progress = progressNodes.Count == 0
            ? 0
            : Math.Round(completedCount * 100m / progressNodes.Count, 2);

        if (progressNodes.Count > 0 && completedCount == progressNodes.Count)
        {
            existingRoadmap.Status = "Completed";
        }
        else
        {
            existingRoadmap.Status = "Active";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var responseNodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(node => node.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
            .Where(node => node.RoadmapId == id)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        return Ok(ToResponse(existingRoadmap, careerRole.Name, responseNodes, false));
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
                    message = "Module lộ trình tiên quyết phải được hoàn thành trước khi bắt đầu module này.",
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

        if (node.SkillId is not null)
        {
            await userSkillSyncService.SyncVerifiedSkillAsync(
                node.Roadmap.UserId,
                node.SkillId.Value,
                node.Roadmap.CareerRoleId,
                GetCurrentUserId(),
                cancellationToken);
        }

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

        var isFullyCompleted = progressNodes.Count > 0 && completedCount == progressNodes.Count;
        if (isFullyCompleted && node.Roadmap.Status != "Completed")
        {
            node.Roadmap.Status = "Completed";
            await dbContext.SaveChangesAsync(cancellationToken);

            await notificationService.SendNotificationAsync(
                node.Roadmap.UserId,
                "RoadmapCompleted",
                "Chúc mừng! Bạn đã hoàn thành lộ trình",
                $"Bạn đã hoàn thành toàn bộ module trong \"{node.Roadmap.Title}\". Làm tốt lắm!",
                "#roadmap",
                null,
                cancellationToken);
        }
        else if (!isFullyCompleted && node.Roadmap.Status == "Completed")
        {
            node.Roadmap.Status = "Active";
        }
    }


    private static RoadmapHierarchy BuildHierarchyNodes(
        Guid roadmapId,
        IReadOnlyList<RoadmapNodeInput> nodeInputs,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> prerequisiteMap,
        DateTimeOffset now)
    {
        var nodes = new List<RoadmapNode>();
        var actionNodes = new List<RoadmapActionNode>();
        var orderIndex = 1;

        // nodeInputs is already topologically ordered; preserve that order via the index
        // so that prerequisite skills (even in other groups) keep their earlier position.
        var groupedInputs = nodeInputs
            .Select((input, index) => new { Input = input, Index = index })
            .GroupBy(item => NormalizeGroupName(item.Input.GroupName))
            .OrderBy(group => group.Min(item => item.Index))
            .ThenBy(group => group.Key);

        var nodeBySkillId = new Dictionary<Guid, RoadmapNode>();

        foreach (var group in groupedInputs)
        {
            var orderedChildren = group
                .OrderBy(item => item.Index)
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
                    PrerequisiteNodeId = null,
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
                if (input.SkillId is not null)
                {
                    nodeBySkillId[input.SkillId.Value] = node;
                }
            }
        }

        // Second pass: wire each node to its nearest semantic prerequisite that is present
        // in this roadmap and placed earlier (Phase A: a single prerequisite link per node).
        foreach (var actionNode in actionNodes)
        {
            var skillId = actionNode.Input.SkillId;
            if (skillId is null || !prerequisiteMap.TryGetValue(skillId.Value, out var prerequisiteSkillIds))
            {
                continue;
            }

            RoadmapNode? nearest = null;
            foreach (var prerequisiteSkillId in prerequisiteSkillIds)
            {
                if (nodeBySkillId.TryGetValue(prerequisiteSkillId, out var prerequisiteNode)
                    && prerequisiteNode.OrderIndex < actionNode.Node.OrderIndex
                    && (nearest is null || prerequisiteNode.OrderIndex > nearest.OrderIndex))
                {
                    nearest = prerequisiteNode;
                }
            }

            if (nearest is not null)
            {
                actionNode.Node.PrerequisiteNodeId = nearest.Id;
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

        // Mức đã xác minh của sinh viên cho từng skill (cận dưới khi lọc tài liệu).
        var verifiedLevelBySkill = await dbContext.UserSkills
            .AsNoTracking()
            .Where(us => us.UserId == GetCurrentUserId() && us.IsVerified && skillIds.Contains(us.SkillId))
            .Select(us => new { us.SkillId, us.VerifiedLevel, us.Level })
            .ToDictionaryAsync(
                us => us.SkillId,
                us => LevelRank(string.IsNullOrWhiteSpace(us.VerifiedLevel) ? us.Level : us.VerifiedLevel),
                cancellationToken);

        var levelBoundsBySkill = reportItems
            .GroupBy(item => item.SkillId)
            .ToDictionary(
                group => group.Key,
                group => (
                    Min: verifiedLevelBySkill.GetValueOrDefault(group.Key, 0),
                    Max: group.Max(item => LevelRank(item.RequiredLevel))));

        var resourcesBySkill = await GetActiveResourcesBySkillAsync(skillIds, cancellationToken, levelBoundsBySkill);

        return reportItems
            .Where(item => !string.Equals(item.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            .Select(item => new RoadmapNodeInput(
                item.SkillId,
                resourcesBySkill.GetValueOrDefault(item.SkillId) ?? [],
                BuildGroupName(item.Skill.Category, item.Priority),
                $"Cải thiện {item.Skill.Name}",
                item.Recommendation ?? $"Đạt cấp độ {item.RequiredLevel} cho {item.Skill.Name}.",
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
        var levelBoundsBySkill = requirements
            .GroupBy(requirement => requirement.SkillId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var max = group.Max(requirement => LevelRank(requirement.RequiredLevel));
                    // Cận dưới = mức đã được xác minh của sinh viên (đã thành thạo).
                    var min = userSkills.TryGetValue(group.Key, out var us) && us.IsVerified
                        ? LevelRank(string.IsNullOrWhiteSpace(us.VerifiedLevel) ? us.Level : us.VerifiedLevel)
                        : 0;
                    return (Min: min, Max: max);
                });
        var resourcesBySkill = await GetActiveResourcesBySkillAsync(requirementSkillIds, cancellationToken, levelBoundsBySkill);

        return requirements
            .Where(requirement => !userSkills.TryGetValue(requirement.SkillId, out var userSkill)
                || !userSkill.IsVerified
                || LevelRank(string.IsNullOrWhiteSpace(userSkill.VerifiedLevel) ? userSkill.Level : userSkill.VerifiedLevel) < LevelRank(requirement.RequiredLevel))
            .Select(requirement =>
            {
                var hasSkill = userSkills.TryGetValue(requirement.SkillId, out var userSkill);
                var priority = hasSkill
                    ? Math.Min(requirement.Priority + 1, 5)
                    : requirement.Priority;
                var status = hasSkill && !userSkill!.IsVerified ? "Cần minh chứng/xác thực." : "Thiếu hoặc kỹ năng yếu.";

                return new RoadmapNodeInput(
                    requirement.SkillId,
                    resourcesBySkill.GetValueOrDefault(requirement.SkillId) ?? [],
                    BuildGroupName(requirement.Skill.Category, priority),
                    $"Học {requirement.Skill.Name}",
                    $"{status} Đạt cấp độ {requirement.RequiredLevel} cho {requirement.Skill.Name}.",
                    "Skill",
                    priority,
                    priority <= 2 ? 12 : 8);
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<Guid>>> GetActiveResourcesBySkillAsync(
        IReadOnlyCollection<Guid> skillIds,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, (int Min, int Max)>? levelBoundsBySkill = null)
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
                resource.Title,
                resource.LessonNumber
            })
            .ToListAsync(cancellationToken);

        return resources
            .GroupBy(resource => resource.SkillId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    // Sắp xếp theo level (độ khó) như một lộ trình tăng dần,
                    // rồi tới số bài học để giữ thứ tự bài trong cùng mức.
                    var ordered = group
                        .OrderBy(resource => DifficultyRank(resource.Difficulty))
                        .ThenBy(resource => resource.LessonNumber)
                        .ThenBy(resource => resource.StorageObjectName == null ? 0 : 1)
                        .ThenBy(resource => resource.Title)
                        .ToList();

                    if (levelBoundsBySkill is not null
                        && levelBoundsBySkill.TryGetValue(group.Key, out var bounds))
                    {
                        var (min, max) = bounds;
                        bool Unknown(string? d) => string.IsNullOrWhiteSpace(d);

                        // Mỗi roadmap chỉ học MỘT level: level đang cần học là mức
                        // ngay trên mức đã được xác minh (min + 1), không vượt mức
                        // yêu cầu của node. Học xong & verify level này thì roadmap
                        // sau mới hiển thị level cao hơn.
                        var targetRank = min + 1;
                        if (max > 0 && targetRank > max)
                        {
                            targetRank = max;
                        }

                        // Ưu tiên: tài liệu đúng level đang học (+ tài liệu không gắn độ khó).
                        var primary = ordered
                            .Where(r => Unknown(r.Difficulty) || DifficultyRank(r.Difficulty) == targetRank)
                            .ToList();

                        // Fallback 1: cả dải (mức đã đạt, mức yêu cầu].
                        var chosen = primary.Count > 0
                            ? primary
                            : ordered
                                .Where(r => Unknown(r.Difficulty)
                                    || (DifficultyRank(r.Difficulty) > min
                                        && (max <= 0 || DifficultyRank(r.Difficulty) <= max)))
                                .ToList();

                        // Fallback 2: giữ nguyên danh sách gốc để node không rỗng.
                        ordered = chosen.Count > 0 ? chosen : ordered;
                    }

                    return (IReadOnlyList<Guid>)ordered
                        .Select(resource => resource.Id)
                        .ToList();
                });
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetSkillPrerequisiteMapAsync(
        IReadOnlyCollection<Guid> skillIds,
        CancellationToken cancellationToken)
    {
        if (skillIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<Guid>>();
        }

        var edges = await dbContext.SkillPrerequisites
            .AsNoTracking()
            .Where(prerequisite => skillIds.Contains(prerequisite.SkillId)
                && skillIds.Contains(prerequisite.PrerequisiteSkillId))
            .Select(prerequisite => new { prerequisite.SkillId, prerequisite.PrerequisiteSkillId })
            .ToListAsync(cancellationToken);

        return edges
            .GroupBy(edge => edge.SkillId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group
                    .Select(edge => edge.PrerequisiteSkillId)
                    .Distinct()
                    .ToList());
    }

    private static List<RoadmapNodeInput> TopologicalOrderInputs(
        IReadOnlyList<RoadmapNodeInput> inputs,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> prerequisiteMap)
    {
        var count = inputs.Count;
        if (count <= 1)
        {
            return inputs.ToList();
        }

        var indegree = new int[count];
        var dependents = new List<int>[count];
        for (var i = 0; i < count; i++)
        {
            dependents[i] = [];
        }

        var indexBySkill = new Dictionary<Guid, int>();
        for (var i = 0; i < count; i++)
        {
            if (inputs[i].SkillId is not null)
            {
                indexBySkill[inputs[i].SkillId!.Value] = i;
            }
        }

        for (var i = 0; i < count; i++)
        {
            var skillId = inputs[i].SkillId;
            if (skillId is null || !prerequisiteMap.TryGetValue(skillId.Value, out var prerequisiteSkillIds))
            {
                continue;
            }

            foreach (var prerequisiteSkillId in prerequisiteSkillIds)
            {
                if (indexBySkill.TryGetValue(prerequisiteSkillId, out var prerequisiteIndex) && prerequisiteIndex != i)
                {
                    dependents[prerequisiteIndex].Add(i);
                    indegree[i]++;
                }
            }
        }

        Comparison<int> byPriorityThenTitle = (left, right) =>
        {
            var byPriority = inputs[left].Priority.CompareTo(inputs[right].Priority);
            return byPriority != 0
                ? byPriority
                : string.Compare(inputs[left].Title, inputs[right].Title, StringComparison.OrdinalIgnoreCase);
        };

        var available = new List<int>();
        for (var i = 0; i < count; i++)
        {
            if (indegree[i] == 0)
            {
                available.Add(i);
            }
        }

        var ordered = new List<RoadmapNodeInput>(count);
        var placed = new bool[count];

        while (available.Count > 0)
        {
            available.Sort(byPriorityThenTitle);
            var next = available[0];
            available.RemoveAt(0);
            ordered.Add(inputs[next]);
            placed[next] = true;

            foreach (var dependent in dependents[next])
            {
                indegree[dependent]--;
                if (indegree[dependent] == 0)
                {
                    available.Add(dependent);
                }
            }
        }

        // Cycle safety: if a dependency cycle exists, append any unplaced nodes by (Priority, Title)
        // so generation never fails or drops a skill.
        if (ordered.Count < count)
        {
            var remaining = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (!placed[i])
                {
                    remaining.Add(i);
                }
            }

            remaining.Sort(byPriorityThenTitle);
            foreach (var index in remaining)
            {
                ordered.Add(inputs[index]);
            }
        }

        return ordered;
    }

    private static List<RoadmapNodeInput> GetFallbackNodes(string careerRoleName)
    {
        var normalizedRole = careerRoleName.ToLowerInvariant();
        var common = new List<RoadmapNodeInput>
        {
            new(null, [], "Foundation", "Làm rõ mong đợi của vai trò mục tiêu", $"Đánh giá các yêu cầu thực tập sinh/fresher cho {careerRoleName} và liệt kê các kỹ năng còn thiếu hàng đầu.", "Reading", 1, 4),
            new(null, [], "Portfolio", "Xây dựng một dự án thực tế cá nhân", $"Tạo một dự án thực tế phù hợp với {careerRoleName} và tài liệu hóa vấn đề, tính năng, công nghệ sử dụng và các bước thiết lập.", "Project", 2, 24),
            new(null, [], "Portfolio", "Cải thiện trình bày GitHub", "Thêm README, ảnh chụp màn hình, hướng dẫn thiết lập, ví dụ API, liên kết triển khai và ghi chú kiến trúc.", "Practice", 3, 6)
        };

        if (normalizedRole.Contains("backend"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Backend Engineering", "Thành thạo nền tảng REST API", "Thiết kế các API CRUD với xác thực, phân trang và phản hồi lỗi rõ ràng.", "Skill", 1, 16),
                new(null, [], "Backend Engineering", "Luyện tập thiết kế cơ sở dữ liệu", "Thiết kế thực thể, mối quan hệ, chỉ mục, migration và các mẫu truy vấn cho một tính năng backend thực tế.", "Skill", 2, 14),
                new(null, [], "Quality", "Viết kiểm thử cho backend", "Viết unit test và integration test cho logic dịch vụ, controller và hành vi cơ sở dữ liệu.", "Practice", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("frontend"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Frontend Engineering", "Củng cố kiến trúc giao diện React", "Xây dựng các component tái sử dụng, bảo vệ định tuyến, xác thực biểu mẫu, trạng thái tải và trạng thái lỗi.", "Skill", 1, 16),
                new(null, [], "Frontend Engineering", "Tích hợp API thực tế", "Kết nối các trang frontend với các API backend được xác thực với xử lý API client sạch sẽ.", "Practice", 2, 12),
                new(null, [], "Quality", "Hoàn thiện UX đáp ứng (responsive)", "Xác minh bố cục trên thiết bị di động và máy tính để bàn, tính khả dụng và các trạng thái trống/lỗi.", "Assessment", 3, 10)
            ]);
        }
        else if (normalizedRole.Contains("devops") || normalizedRole.Contains("cloud"))
        {
            common.InsertRange(1,
            [
                new(null, [], "DevOps Foundation", "Container hóa ứng dụng", "Viết Dockerfile và cấu hình docker-compose cho backend, frontend và cơ sở dữ liệu.", "Project", 1, 12),
                new(null, [], "DevOps Foundation", "Xây dựng luồng CI/CD", "Tự động hóa các bước biên dịch, kiểm thử, migration và triển khai.", "Practice", 2, 14),
                new(null, [], "Operations", "Thêm giám sát cơ bản", "Theo dõi nhật ký hệ thống (logs), kiểm tra trạng thái hoạt động (health checks), trạng thái triển khai và quy trình hoàn tác (rollback).", "Skill", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("data"))
        {
            common.InsertRange(1,
            [
                new(null, [], "Data Foundation", "Luyện tập SQL và mô hình hóa dữ liệu", "Thiết kế giản đồ phân tích, biến đổi dữ liệu và các ví dụ tối ưu hóa truy vấn.", "Skill", 1, 16),
                new(null, [], "Data Engineering", "Xây dựng luồng dữ liệu (data pipeline)", "Thu thập, làm sạch, biến đổi và xuất tập dữ liệu với các tập lệnh có thể tái tạo.", "Project", 2, 20),
                new(null, [], "Quality", "Tài liệu hóa kiểm tra chất lượng dữ liệu", "Thêm các quy tắc xác thực, lập hồ sơ dữ liệu và xử lý lỗi luồng dữ liệu.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("qa"))
        {
            common.InsertRange(1,
            [
                new(null, [], "QA Foundation", "Viết chiến lược kiểm thử", "Xác định phạm vi kiểm thử, tiêu chí chấp nhận và rủi ro hồi quy cho một sản phẩm mẫu.", "Reading", 1, 8),
                new(null, [], "QA Automation", "Tự động hóa kiểm kiểm API và UI", "Tạo các bài kiểm thử có thể chạy lại cho luồng người dùng chính và hợp đồng API.", "Project", 2, 18),
                new(null, [], "Quality", "Thiết lập báo cáo kiểm thử", "Công bố kết quả kiểm thử và tài liệu hóa lỗi với các bước tái dựng.", "Practice", 3, 8)
            ]);
        }
        else if (normalizedRole.Contains("ai"))
        {
            common.InsertRange(1,
            [
                new(null, [], "AI Engineering", "Học tích hợp API AI", "Xây dựng prompt, đầu ra có cấu trúc, thử lại và xử lý lỗi xung quanh nhà cung cấp AI.", "Skill", 1, 14),
                new(null, [], "AI Engineering", "Xây dựng tính năng hỗ trợ bởi AI", "Tạo một tính năng sử dụng ngữ cảnh người dùng và lưu trữ kết quả tạo ra.", "Project", 2, 20),
                new(null, [], "Quality", "Đánh giá chất lượng đầu ra của AI", "Thêm xác thực, kiểm tra an toàn và thu thập phản hồi cho nội dung được tạo.", "Assessment", 3, 10)
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
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
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
        $"Hoàn thành các module {groupName.ToLowerInvariant()} theo thứ tự ưu tiên trước khi chuyển sang phần học tiếp theo.";

    private static RoadmapResponse ToResponse(Roadmap roadmap, string careerRoleName, IReadOnlyList<RoadmapNode> nodes, bool isOutdated) =>
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
            BuildNodeTree(nodes),
            isOutdated);

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
            resource.EstimatedHours,
            resource.LessonNumber);

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
    IReadOnlyList<RoadmapNodeResponse> NodeTree,
    bool IsOutdated);

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
    int? EstimatedHours,
    int LessonNumber);
