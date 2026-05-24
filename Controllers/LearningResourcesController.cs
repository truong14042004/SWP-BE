using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/learning-resources")]
public sealed class LearningResourcesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentLearningResourceResponse>>> GetLearningResources(
        Guid? skillId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LearningResources
            .AsNoTracking()
            .Include(resource => resource.Skill)
            .Where(resource => resource.IsActive);

        if (skillId is not null)
        {
            query = query.Where(resource => resource.SkillId == skillId);
        }

        var resources = await query
            .OrderBy(resource => resource.Skill == null ? null : resource.Skill.Category)
            .ThenBy(resource => resource.Skill == null ? null : resource.Skill.Name)
            .ThenBy(resource => resource.LessonNumber)
            .ThenBy(resource => resource.Title)
            .Select(resource => ToResponse(resource))
            .ToListAsync(cancellationToken);

        return Ok(resources);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentLearningResourceResponse>> GetLearningResource(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.LearningResources
            .AsNoTracking()
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);

        return resource is null
            ? NotFound(new { message = "Không tìm thấy tài nguyên học tập." })
            : Ok(ToResponse(resource));
    }

    private static StudentLearningResourceResponse ToResponse(LearningResource resource) =>
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
}

public sealed record StudentLearningResourceResponse(
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
