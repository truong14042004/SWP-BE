using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.Profile;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(item => item.TargetRole)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profile was not found." });
        }

        return Ok(ToResponse(profile));
    }

    [HttpPost]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> CreateProfile(
        SaveProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var exists = await dbContext.StudentProfiles.AnyAsync(
            item => item.UserId == userId,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Profile already exists for this user." });
        }

        var validationError = await ValidateProfileRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var profile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyProfileValues(profile, request);
        dbContext.StudentProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(profile).Reference(item => item.TargetRole).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProfile), ToResponse(profile));
    }

    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        SaveProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await dbContext.StudentProfiles
            .Include(item => item.TargetRole)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return NotFound(new { message = "Profile was not found." });
        }

        var validationError = await ValidateProfileRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        ApplyProfileValues(profile, request);
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(profile));
    }

    private static void ApplyProfileValues(StudentProfile profile, SaveProfileRequest request)
    {
        profile.School = request.School?.Trim();
        profile.Major = request.Major?.Trim();
        profile.Year = request.Year;
        profile.Gpa = request.Gpa;
        profile.TargetRoleId = request.TargetRoleId;
        profile.GithubUsername = request.GithubUsername?.Trim();
        profile.CareerGoal = request.CareerGoal?.Trim();
        profile.PreferredLearningHoursPerWeek = request.PreferredLearningHoursPerWeek;
    }

    private async Task<string?> ValidateProfileRequest(
        SaveProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.School is { Length: > 200 })
        {
            return "School must be at most 200 characters.";
        }

        if (request.Major is { Length: > 200 })
        {
            return "Major must be at most 200 characters.";
        }

        if (request.GithubUsername is { Length: > 100 })
        {
            return "GitHub username must be at most 100 characters.";
        }

        if (request.CareerGoal is { Length: > 1000 })
        {
            return "Career goal must be at most 1000 characters.";
        }

        if (request.Year is < 1 or > 8)
        {
            return "Year must be between 1 and 8.";
        }

        if (request.Gpa is < 0)
        {
            return "GPA must be greater than or equal to 0.";
        }

        if (request.PreferredLearningHoursPerWeek is < 0)
        {
            return "Preferred learning hours per week must be greater than or equal to 0.";
        }

        if (request.TargetRoleId is not null)
        {
            var careerRoleExists = await dbContext.CareerRoles.AnyAsync(
                role => role.Id == request.TargetRoleId && role.IsActive,
                cancellationToken);
            if (!careerRoleExists)
            {
                return "Active career role was not found.";
            }
        }

        return null;
    }

    private static ProfileResponse ToResponse(StudentProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
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
            profile.UpdatedAt);

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }
}
