using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Controllers;

namespace SWP_BE.Services;

public sealed class RoadmapMaterializer : IRoadmapMaterializer
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RoadmapMaterializer> _logger;
    private readonly IRoadmapResourceProvisioner _resourceProvisioner;

    public RoadmapMaterializer(
        AppDbContext dbContext,
        ILogger<RoadmapMaterializer> logger,
        IRoadmapResourceProvisioner resourceProvisioner)
    {
        _dbContext = dbContext;
        _logger = logger;
        _resourceProvisioner = resourceProvisioner;
    }

    public async Task<MaterializeResult> MaterializeRoadmapAsync(
        Guid userId,
        AiRoadmapDto roadmapDto,
        CancellationToken cancellationToken)
    {
        // Find a career role to attach: prefer the role the AI hinted at, else student profile target role,
        // else fall back to any active role.
        Guid? targetRoleId = null;

        if (!string.IsNullOrWhiteSpace(roadmapDto.CareerRoleHint))
        {
            var hint = roadmapDto.CareerRoleHint.Trim().ToLowerInvariant();
            targetRoleId = await _dbContext.CareerRoles
                .AsNoTracking()
                .Where(role => role.IsActive && role.Name.ToLower().Contains(hint))
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!targetRoleId.HasValue)
        {
            var profile = await _dbContext.StudentProfiles
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
            targetRoleId = await _dbContext.CareerRoles
                .AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.Name)
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!targetRoleId.HasValue)
        {
            throw new InvalidOperationException("Không có định hướng nghề nghiệp nào đang hoạt động để liên kết với lộ trình.");
        }

        var title = string.IsNullOrWhiteSpace(roadmapDto.Title)
            ? "Roadmap đề xuất bởi AI Mentor"
            : roadmapDto.Title.Trim();

        var titleLower = title.ToLowerInvariant();
        var existingRoadmap = await _dbContext.Roadmaps
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Title.ToLower() == titleLower, cancellationToken);

        if (existingRoadmap is not null)
        {
            var existingNodeCount = await _dbContext.RoadmapNodes
                .CountAsync(n => n.RoadmapId == existingRoadmap.Id, cancellationToken);

            return new MaterializeResult(
                existingRoadmap.Id,
                existingRoadmap.Title,
                existingNodeCount,
                true);
        }

        var now = DateTimeOffset.UtcNow;
        var roadmap = new Roadmap
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareerRoleId = targetRoleId.Value,
            Title = title,
            Description = roadmapDto.Description?.Trim(),
            Status = "Active",
            Progress = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Roadmaps.Add(roadmap);

        var skills = await _dbContext.Skills
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Id, item.Name, item.Category })
            .ToListAsync(cancellationToken);
        var categoryTitles = skills.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var categorySet = categoryTitles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skillIdsByTitle = skills
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        var resources = await _dbContext.LearningResources
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

        // Chỉ chọn tài liệu ĐÚNG mức sinh viên đang cần học (mức verified + 1),
        // không gắn cả tài liệu Beginner lẫn Advanced vào cùng 1 node.
        var verifiedLevelBySkill = await _dbContext.UserSkills
            .AsNoTracking()
            .Where(us => us.UserId == userId && us.IsVerified)
            .Select(us => new { us.SkillId, us.VerifiedLevel, us.Level })
            .ToDictionaryAsync(
                us => us.SkillId,
                us => LevelRank(string.IsNullOrWhiteSpace(us.VerifiedLevel) ? us.Level : us.VerifiedLevel),
                cancellationToken);

        var requiredLevelBySkill = await _dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(item => item.CareerRoleId == targetRoleId.Value)
            .ToDictionaryAsync(item => item.SkillId, item => LevelRank(item.RequiredLevel), cancellationToken);

        var resourceIdsBySkill = resources
            .Where(item => item.SkillId is not null)
            .GroupBy(item => item.SkillId!.Value)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var ordered = group
                        .OrderBy(item => DifficultyRank(item.Difficulty))
                        .ThenBy(item => item.LessonNumber)
                        .ThenBy(item => item.StorageObjectName == null ? 0 : 1)
                        .ThenBy(item => item.Title)
                        .ToList();

                    var min = verifiedLevelBySkill.GetValueOrDefault(group.Key, 0);
                    var max = requiredLevelBySkill.GetValueOrDefault(group.Key, 0);
                    var targetRank = min + 1;
                    if (max > 0 && targetRank > max)
                    {
                        targetRank = max;
                    }

                    bool Unknown(string? d) => string.IsNullOrWhiteSpace(d);

                    var primary = ordered
                        .Where(item => Unknown(item.Difficulty) || DifficultyRank(item.Difficulty) == targetRank)
                        .ToList();

                    var chosen = primary.Count > 0
                        ? primary
                        : ordered
                            .Where(item => Unknown(item.Difficulty)
                                || (DifficultyRank(item.Difficulty) > min && (max <= 0 || DifficultyRank(item.Difficulty) <= max)))
                            .ToList();

                    return (IReadOnlyList<Guid>)(chosen.Count > 0 ? chosen : ordered)
                        .Select(item => item.Id)
                        .ToList();
                });

        var allowedModuleTitles = skillIdsByTitle.Keys
            .Concat(resourceIdsByTitle.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sanitizedNodes = SanitizeRoadmapCategories(roadmapDto.Nodes, categorySet, allowedModuleTitles);

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
            throw new InvalidOperationException("Lộ trình phải chứa ít nhất một module.");
        }

        // FR2.3: đảm bảo mỗi technical node (không phải Group) có tối thiểu 2 tài nguyên học tập.
        var existingCountByNode = nodeResources
            .GroupBy(item => item.RoadmapNodeId)
            .ToDictionary(group => group.Key, group => group.Count());
        // Tập resource đã gắn cho mỗi node để loại trùng (unique index NodeId, ResourceId).
        var existingResourceIdsByNode = nodeResources
            .GroupBy(item => item.RoadmapNodeId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.LearningResourceId).ToHashSet());
        var topUpContexts = nodes
            .Where(node => !node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
            .Select(node => new NodeResourceContext(
                node.Id,
                node.SkillId,
                node.Title,
                existingCountByNode.GetValueOrDefault(node.Id, 0)))
            .ToList();
        var topUp = await _resourceProvisioner.EnsureMinimumResourcesAsync(
            topUpContexts, 2, now, cancellationToken);
        foreach (var node in nodes)
        {
            if (!topUp.TryGetValue(node.Id, out var extraIds) || extraIds.Count == 0)
            {
                continue;
            }

            var alreadyOnNode = existingResourceIdsByNode.GetValueOrDefault(node.Id, []);
            var newIds = extraIds.Where(id => alreadyOnNode.Add(id)).ToList();
            if (newIds.Count == 0)
            {
                continue;
            }

            var startIndex = existingCountByNode.GetValueOrDefault(node.Id, 0);
            for (var i = 0; i < newIds.Count; i++)
            {
                nodeResources.Add(new RoadmapNodeResource
                {
                    Id = Guid.NewGuid(),
                    RoadmapNodeId = node.Id,
                    LearningResourceId = newIds[i],
                    OrderIndex = startIndex + i + 1,
                    CreatedAt = now
                });
            }

            node.LearningResourceId ??= newIds[0];
        }

        try
        {
            _dbContext.RoadmapNodes.AddRange(nodes);
            _dbContext.RoadmapNodeResources.AddRange(nodeResources);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            _logger.LogError(dbEx, "Failed to apply AI roadmap for user {UserId}. Inner: {Inner}", userId, inner);
            throw;
        }

        return new MaterializeResult(
            roadmap.Id,
            roadmap.Title,
            nodes.Count,
            false);
    }

    public static IReadOnlyList<AiRoadmapNodeDto> SanitizeRoadmapCategories(
        IReadOnlyList<AiRoadmapNodeDto>? source,
        HashSet<string> validCategories,
        HashSet<string> allowedModuleTitles)
    {
        if (source is null) return Array.Empty<AiRoadmapNodeDto>();
        var list = new List<AiRoadmapNodeDto>();

        foreach (var item in source)
        {
            var title = item.Title?.Trim();
            if (string.IsNullOrEmpty(title)) continue;

            var nodeType = item.NodeType?.Trim();
            var isGroup = string.Equals(nodeType, "Group", StringComparison.OrdinalIgnoreCase);

            if (isGroup)
            {
                if (validCategories.Contains(title))
                {
                    var childList = SanitizeRoadmapCategories(item.Children, validCategories, allowedModuleTitles);
                    if (childList.Count > 0)
                    {
                        list.Add(item with { Title = title, Children = childList });
                    }
                }
            }
            else
            {
                if (allowedModuleTitles.Contains(title))
                {
                    list.Add(item with { Title = title, Children = null });
                }
            }
        }

        return list;
    }

    private static void FlattenNodes(
        IReadOnlyList<AiRoadmapNodeDto>? items,
        Guid roadmapId,
        Guid? parentId,
        int level,
        ref int globalOrder,
        DateTimeOffset now,
        List<RoadmapNode> output,
        List<RoadmapNodeResource> nodeResources,
        Dictionary<string, Guid> skillIdsByTitle,
        Dictionary<string, Guid> resourceIdsByTitle,
        Dictionary<Guid, IReadOnlyList<Guid>> resourceIdsBySkill)
    {
        if (items is null) return;

        foreach (var item in items)
        {
            var title = item.Title?.Trim();
            if (string.IsNullOrEmpty(title)) continue;

            var nodeType = item.NodeType?.Trim() ?? "Leaf";
            var isGroup = string.Equals(nodeType, "Group", StringComparison.OrdinalIgnoreCase);

            Guid? skillId = null;
            var relatedResourceIds = new List<Guid>();

            if (isGroup)
            {
                nodeType = "Group";
            }
            else
            {
                nodeType = "Leaf";
                if (skillIdsByTitle.TryGetValue(title, out var sId))
                {
                    skillId = sId;
                    if (resourceIdsBySkill.TryGetValue(sId, out var skillResources))
                    {
                        relatedResourceIds.AddRange(skillResources);
                    }
                }
                else if (resourceIdsByTitle.TryGetValue(title, out var resId))
                {
                    relatedResourceIds.Add(resId);
                }
            }

            var safeLevel = level < 0 ? 0 : level;
            var priority = item.Priority switch
            {
                null => 5,
                <= 0 => 5,
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
        "beginner" or "basic" or "fundamental" or "fundamentals" or "cơ bản" => 1,
        "intermediate" or "trung cấp" => 2,
        "advanced" or "nâng cao" => 3,
        "expert" => 4,
        _ => 5
    };

    private static int LevelRank(string? level) => level?.Trim().ToLowerInvariant() switch
    {
        "verified" => 4,
        "advanced" => 3,
        "intermediate" => 2,
        "beginner" => 1,
        _ => 0
    };
}
