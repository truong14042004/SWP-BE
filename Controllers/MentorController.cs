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
[Route("api/mentor")]
public sealed class MentorController(
    AppDbContext dbContext,
    IAiTextGenerationService aiTextGenerationService) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<MentorSessionResponse>> Chat(
        MentorChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "Question is required." });
        }

        var userId = GetCurrentUserId();
        var context = await BuildMentorContext(userId, request.ContextJson, cancellationToken);
        AiTextResult aiResult;
        try
        {
            aiResult = await aiTextGenerationService.GenerateAsync(
                "You are an AI career mentor for software engineering students. Give practical, concise, structured guidance in Vietnamese. Base the answer on the provided student profile, skills, target role, skill gap, roadmap, GitHub and portfolio context. Do not invent facts not present in context.",
                $"""
                Student context:
                {context}

                Student question:
                {request.Question.Trim()}
                """,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }

        var now = DateTimeOffset.UtcNow;
        var session = new MentorSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Question = request.Question.Trim(),
            Answer = aiResult.Text,
            ContextJson = context,
            Model = aiResult.Model,
            TokensUsed = aiResult.TokensUsed,
            CreatedAt = now
        };

        dbContext.MentorSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(session));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<MentorSessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sessions = await dbContext.MentorSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => ToResponse(session))
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<ActionResult<MentorSessionResponse>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var session = await dbContext.MentorSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (session is null)
        {
            return NotFound(new { message = "Mentor session was not found." });
        }

        return Ok(ToResponse(session));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private async Task<string> BuildMentorContext(
        Guid userId,
        string? extraContext,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(item => item.TargetRole)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var skills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(item => item.Skill)
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Skill.Category)
            .ThenBy(item => item.Skill.Name)
            .Select(item => $"{item.Skill.Name} ({item.Skill.Category}): {item.Level}, verified={item.IsVerified}")
            .ToListAsync(cancellationToken);

        var latestGap = await dbContext.SkillGapReports
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.MatchScore,
                item.Summary,
                CareerRole = item.CareerRole.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        var gapItems = latestGap is null
            ? []
            : await dbContext.SkillGapReportItems
                .AsNoTracking()
                .Include(item => item.Skill)
                .Where(item => item.SkillGapReportId == latestGap.Id)
                .OrderBy(item => item.Priority)
                .Select(item => $"{item.Skill.Name}: current={item.CurrentLevel ?? "Missing"}, required={item.RequiredLevel}, status={item.Status}, priority={item.Priority}, recommendation={item.Recommendation}")
                .ToListAsync(cancellationToken);

        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Status,
                item.Progress
            })
            .FirstOrDefaultAsync(cancellationToken);

        var roadmapNodes = roadmap is null
            ? []
            : await dbContext.RoadmapNodes
                .AsNoTracking()
                .Where(item => item.RoadmapId == roadmap.Id)
                .OrderBy(item => item.OrderIndex)
                .Select(item => $"{item.OrderIndex}. {item.Title} - {item.Status}, priority={item.Priority}, hours={item.EstimatedHours}")
                .ToListAsync(cancellationToken);

        var repos = await dbContext.GithubRepositories
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.QualityScore)
            .Take(5)
            .Select(item => $"{item.RepoName}: language={item.MainLanguage}, score={item.QualityScore}, summary={item.AiSummary}")
            .ToListAsync(cancellationToken);

        return $$"""
        Profile:
        school={{profile?.School ?? "unknown"}}
        major={{profile?.Major ?? "unknown"}}
        year={{profile?.Year?.ToString() ?? "unknown"}}
        gpa={{profile?.Gpa?.ToString() ?? "unknown"}}
        targetRole={{profile?.TargetRole?.Name ?? "unknown"}}
        careerGoal={{profile?.CareerGoal ?? "unknown"}}
        preferredHoursPerWeek={{profile?.PreferredLearningHoursPerWeek?.ToString() ?? "unknown"}}

        Skills:
        {{string.Join("\n", skills.DefaultIfEmpty("No skills entered."))}}

        Latest skill gap:
        role={{latestGap?.CareerRole ?? "unknown"}}
        matchScore={{latestGap?.MatchScore.ToString() ?? "unknown"}}
        summary={{latestGap?.Summary ?? "unknown"}}
        items={{string.Join("\n", gapItems.DefaultIfEmpty("No gap items."))}}

        Latest roadmap:
        {{(roadmap is null ? "No roadmap." : $"{roadmap.Title}, status={roadmap.Status}, progress={roadmap.Progress}")}}
        {{string.Join("\n", roadmapNodes.DefaultIfEmpty("No roadmap nodes."))}}

        GitHub repositories:
        {{string.Join("\n", repos.DefaultIfEmpty("No repositories analyzed."))}}

        Extra context:
        {{(string.IsNullOrWhiteSpace(extraContext) ? "none" : extraContext.Trim())}}
        """;
    }

    private static MentorSessionResponse ToResponse(MentorSession session) =>
        new(
            session.Id,
            session.Question,
            session.Answer,
            session.ContextJson,
            session.Model,
            session.TokensUsed,
            session.CreatedAt);
}

public sealed record MentorChatRequest(string Question, string? ContextJson);

public sealed record MentorSessionResponse(
    Guid Id,
    string Question,
    string Answer,
    string? ContextJson,
    string? Model,
    int? TokensUsed,
    DateTimeOffset CreatedAt);
