using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Student)]
public sealed class LessonProgressController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("api/roadmap/{roadmapId:guid}/lesson-progress")]
    public async Task<ActionResult<IReadOnlyList<LessonProgressResponse>>> GetForRoadmap(
        Guid roadmapId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var roadmapBelongsToUser = await dbContext.Roadmaps
            .AsNoTracking()
            .AnyAsync(item => item.Id == roadmapId && item.UserId == userId, cancellationToken);

        if (!roadmapBelongsToUser)
        {
            return NotFound(new { message = "Không tìm thấy roadmap." });
        }

        var items = await dbContext.LessonProgresses
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && dbContext.RoadmapNodes.Any(node =>
                    node.Id == item.RoadmapNodeId && node.RoadmapId == roadmapId))
            .Select(item => new LessonProgressResponse(
                item.RoadmapNodeId,
                item.LearningResourceId,
                item.CompletedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("api/roadmap-node/{nodeId:guid}/lessons/{lessonId:guid}/complete")]
    public async Task<ActionResult<LessonProgressResponse>> MarkComplete(
        Guid nodeId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var node = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(item => item.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == nodeId, cancellationToken);

        if (node is null || node.Roadmap.UserId != userId)
        {
            return NotFound(new { message = "Không tìm thấy module roadmap." });
        }

        var lessonExists = await dbContext.LearningResources
            .AsNoTracking()
            .AnyAsync(item => item.Id == lessonId, cancellationToken);

        if (!lessonExists)
        {
            return NotFound(new { message = "Không tìm thấy bài học." });
        }

        var existing = await dbContext.LessonProgresses
            .SingleOrDefaultAsync(item => item.UserId == userId
                && item.RoadmapNodeId == nodeId
                && item.LearningResourceId == lessonId, cancellationToken);

        if (existing is not null)
        {
            return Ok(new LessonProgressResponse(
                existing.RoadmapNodeId,
                existing.LearningResourceId,
                existing.CompletedAt));
        }

        var progress = new LessonProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoadmapNodeId = nodeId,
            LearningResourceId = lessonId,
            CompletedAt = DateTimeOffset.UtcNow
        };

        dbContext.LessonProgresses.Add(progress);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new LessonProgressResponse(
            progress.RoadmapNodeId,
            progress.LearningResourceId,
            progress.CompletedAt));
    }

    [HttpDelete("api/roadmap-node/{nodeId:guid}/lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> Unmark(
        Guid nodeId,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var existing = await dbContext.LessonProgresses
            .SingleOrDefaultAsync(item => item.UserId == userId
                && item.RoadmapNodeId == nodeId
                && item.LearningResourceId == lessonId, cancellationToken);

        if (existing is null)
        {
            return NoContent();
        }

        dbContext.LessonProgresses.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }
}

public sealed record LessonProgressResponse(
    Guid RoadmapNodeId,
    Guid LearningResourceId,
    DateTimeOffset CompletedAt);
