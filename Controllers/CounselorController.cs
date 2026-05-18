using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.AcademicCounselor)]
[Route("api/counselor")]
public sealed class CounselorController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/counselor/students
    // Lấy danh sách tất cả sinh viên (role = Student, isActive = true)
    [HttpGet("students")]
    [ProducesResponseType<IReadOnlyList<CounselorStudentSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CounselorStudentSummaryResponse>>> GetStudents(
        CancellationToken cancellationToken)
    {
        var students = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRoles.Student && user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new CounselorStudentSummaryResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.Username,
                user.AvatarUrl,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // GET /api/counselor/students/{studentId}/profile
    // Lấy profile chi tiết của một sinh viên
    [HttpGet("students/{studentId:guid}/profile")]
    [ProducesResponseType<CounselorStudentProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CounselorStudentProfileResponse>> GetStudentProfile(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var profile = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(p => p.TargetRole)
            .SingleOrDefaultAsync(p => p.UserId == studentId, cancellationToken);

        return Ok(new CounselorStudentProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Username,
            user.AvatarUrl,
            user.CreatedAt,
            profile is null ? null : new CounselorProfileDetailsResponse(
                profile.Id,
                profile.School,
                profile.Major,
                profile.Year,
                profile.Gpa,
                profile.TargetRoleId,
                profile.TargetRole?.Name,
                profile.GithubUsername,
                profile.CareerGoal,
                profile.PreferredLearningHoursPerWeek,
                profile.CreatedAt,
                profile.UpdatedAt)));
    }

    // GET /api/counselor/students/{studentId}/skills
    // Lấy danh sách kỹ năng của một sinh viên
    [HttpGet("students/{studentId:guid}/skills")]
    [ProducesResponseType<IReadOnlyList<CounselorStudentSkillResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CounselorStudentSkillResponse>>> GetStudentSkills(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var skills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(us => us.Skill)
            .Include(us => us.VerifiedByUser)
            .Where(us => us.UserId == studentId)
            .OrderBy(us => us.Skill.Category)
            .ThenBy(us => us.Skill.Name)
            .Select(us => new CounselorStudentSkillResponse(
                us.Id,
                us.SkillId,
                us.Skill.Name,
                us.Skill.Category,
                us.Level,
                us.IsVerified,
                us.VerifiedByUserId,
                us.VerifiedByUser != null ? us.VerifiedByUser.FullName : null,
                us.EvidenceUrl,
                us.EvidenceType,
                us.CreatedAt,
                us.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(skills);
    }

    // GET /api/counselor/students/{studentId}/skill-gap
    // Lấy báo cáo skill gap gần nhất của một sinh viên
    [HttpGet("students/{studentId:guid}/skill-gap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentSkillGap(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var report = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = "No skill gap report found for this student." });
        }

        var items = await dbContext.SkillGapReportItems
            .AsNoTracking()
            .Include(i => i.Skill)
            .Where(i => i.SkillGapReportId == report.Id)
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Skill.Name)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            report.Id,
            report.UserId,
            report.CareerRoleId,
            CareerRoleName = report.CareerRole.Name,
            report.MatchScore,
            report.Summary,
            report.CreatedAt,
            Items = items.Select(i => new
            {
                i.SkillId,
                SkillName = i.Skill.Name,
                SkillCategory = i.Skill.Category,
                i.CurrentLevel,
                i.RequiredLevel,
                i.Status,
                i.Priority,
                i.Recommendation
            })
        });
    }

    // GET /api/counselor/students/{studentId}/roadmap
    // Lấy roadmap gần nhất của một sinh viên
    [HttpGet("students/{studentId:guid}/roadmap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentRoadmap(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmap is null)
        {
            return NotFound(new { message = "No roadmap found for this student." });
        }

        var nodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(n => n.Skill)
            .Where(n => n.RoadmapId == roadmap.Id)
            .OrderBy(n => n.OrderIndex)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            roadmap.Id,
            roadmap.UserId,
            roadmap.CareerRoleId,
            CareerRoleName = roadmap.CareerRole.Name,
            roadmap.SkillGapReportId,
            roadmap.Title,
            roadmap.Description,
            roadmap.Status,
            roadmap.Progress,
            roadmap.CreatedAt,
            roadmap.UpdatedAt,
            Nodes = nodes.Select(n => new
            {
                n.Id,
                n.SkillId,
                SkillName = n.Skill != null ? n.Skill.Name : null,
                n.ParentNodeId,
                n.PrerequisiteNodeId,
                n.Title,
                n.Description,
                n.NodeType,
                n.Status,
                n.Level,
                n.OrderIndex,
                n.EstimatedHours,
                n.Priority
            })
        });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record CounselorStudentSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? AvatarUrl,
    DateTimeOffset CreatedAt);

public sealed record CounselorStudentProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? AvatarUrl,
    DateTimeOffset UserCreatedAt,
    CounselorProfileDetailsResponse? Profile);

public sealed record CounselorProfileDetailsResponse(
    Guid Id,
    string? School,
    string? Major,
    int? Year,
    decimal? Gpa,
    Guid? TargetRoleId,
    string? TargetRoleName,
    string? GithubUsername,
    string? CareerGoal,
    int? PreferredLearningHoursPerWeek,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CounselorStudentSkillResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    bool IsVerified,
    Guid? VerifiedByUserId,
    string? VerifiedByFullName,
    string? EvidenceUrl,
    string? EvidenceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
