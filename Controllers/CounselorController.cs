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
