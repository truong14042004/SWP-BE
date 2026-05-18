using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public sealed class SearchController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/search/skills?q=keyword
    // Tìm kiếm kỹ năng theo keyword tên, phục vụ autocomplete
    [HttpGet("skills")]
    [ProducesResponseType<IReadOnlyList<SkillSearchResultResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillSearchResultResponse>>> SearchSkills(
        [FromQuery] string? q,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var query = dbContext.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(skill =>
                skill.Name.ToLower().Contains(keyword) ||
                skill.Category.ToLower().Contains(keyword));
        }

        var skills = await query
            .OrderBy(skill => skill.Category)
            .ThenBy(skill => skill.Name)
            .Take(limit)
            .Select(skill => new SkillSearchResultResponse(
                skill.Id,
                skill.Name,
                skill.Category,
                skill.Description))
            .ToListAsync(cancellationToken);

        return Ok(skills);
    }

    // GET /api/search/learning-resources?q=keyword&skillId=...
    // Tìm kiếm tài nguyên học tập theo keyword và/hoặc skillId
    [HttpGet("learning-resources")]
    [ProducesResponseType<IReadOnlyList<LearningResourceSearchResultResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LearningResourceSearchResultResponse>>> SearchLearningResources(
        [FromQuery] string? q,
        [FromQuery] Guid? skillId,
        [FromQuery] string? resourceType,
        [FromQuery] string? difficulty,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var query = dbContext.LearningResources
            .AsNoTracking()
            .Include(r => r.Skill)
            .Where(r => r.IsActive);

        // Filter by skillId
        if (skillId is not null)
        {
            query = query.Where(r => r.SkillId == skillId);
        }

        // Filter by resourceType
        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            var rt = resourceType.Trim();
            query = query.Where(r => r.ResourceType == rt);
        }

        // Filter by difficulty
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var diff = difficulty.Trim();
            query = query.Where(r => r.Difficulty == diff);
        }

        // Keyword search across title, skill name and skill category
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(keyword) ||
                (r.Skill != null && r.Skill.Name.ToLower().Contains(keyword)) ||
                (r.Skill != null && r.Skill.Category.ToLower().Contains(keyword)));
        }

        var resources = await query
            .OrderBy(r => r.Skill == null ? null : r.Skill.Category)
            .ThenBy(r => r.Skill == null ? null : r.Skill.Name)
            .ThenBy(r => r.Title)
            .Take(limit)
            .Select(r => new LearningResourceSearchResultResponse(
                r.Id,
                r.SkillId,
                r.Skill != null ? r.Skill.Name : null,
                r.Skill != null ? r.Skill.Category : null,
                r.Title,
                r.Url,
                r.StorageObjectName == null ? "Link" : "File",
                r.ContentType,
                r.ResourceType,
                r.Difficulty,
                r.EstimatedHours))
            .ToListAsync(cancellationToken);

        return Ok(resources);
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record SkillSearchResultResponse(
    Guid Id,
    string Name,
    string Category,
    string? Description);

public sealed record LearningResourceSearchResultResponse(
    Guid Id,
    Guid? SkillId,
    string? SkillName,
    string? SkillCategory,
    string Title,
    string Url,
    string SourceType,
    string? ContentType,
    string ResourceType,
    string? Difficulty,
    int? EstimatedHours);
