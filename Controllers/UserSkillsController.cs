using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.UserSkills;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/user-skills")]
public sealed class UserSkillsController(AppDbContext dbContext) : ControllerBase
{
    private static readonly string[] AllowedLevels =
    [
        "Beginner",
        "Intermediate",
        "Advanced",
        "Verified"
    ];

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSkillResponse>>(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserSkillResponse>>> GetUserSkills( //tra ve danh sach ky nang cua user
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userSkills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(item => item.Skill) //nap luon bang skill de lay ten va loai ky nang
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Skill.Category)
            .ThenBy(item => item.Skill.Name)
            .ToListAsync(cancellationToken);

        return Ok(userSkills.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillResponse>> CreateUserSkill(
        CreateUserSkillRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = GetCurrentUserId();
        var skill = await dbContext.Skills
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.SkillId && item.IsActive, //kiem tra xem skill co ton tai va active khong
                cancellationToken);
        if (skill is null)
        {
            return BadRequest(new { message = "Active skill was not found." });
        }

        var duplicate = await dbContext.UserSkills.AnyAsync(
            item => item.UserId == userId && item.SkillId == request.SkillId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "This skill is already added for the current user." });
        }

        var now = DateTimeOffset.UtcNow;
        var userSkill = new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillId = request.SkillId,
            Level = NormalizeLevel(request.Level), //chuan hoa chu hoa/thuong
            EvidenceUrl = request.EvidenceUrl?.Trim(),
            EvidenceType = request.EvidenceType?.Trim(),
            IsVerified = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.UserSkills.Add(userSkill);
        await dbContext.SaveChangesAsync(cancellationToken);

        userSkill.Skill = skill;

        return CreatedAtAction(nameof(GetUserSkills), ToResponse(userSkill));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSkillResponse>> UpdateUserSkill(
        Guid id,
        UpdateUserSkillRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Level is not null)
        {
            var levelError = ValidateLevel(request.Level);
            if (levelError is not null)
            {
                return BadRequest(new { message = levelError }); 
            }
        }

        var evidenceError = ValidateEvidenceFields(request.EvidenceUrl, request.EvidenceType);
        if (evidenceError is not null)
        {
            return BadRequest(new { message = evidenceError });
        }

        var userId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "User skill was not found." });
        }

        if (request.Level is not null)
        {
            userSkill.Level = NormalizeLevel(request.Level);
        }

        if (request.EvidenceUrl is not null)
        {
            userSkill.EvidenceUrl = string.IsNullOrWhiteSpace(request.EvidenceUrl) 
                ? null //trong thi gan null (tuc la xoa URL bang chung)
                : request.EvidenceUrl.Trim();
        }

        if (request.EvidenceType is not null)
        {
            userSkill.EvidenceType = string.IsNullOrWhiteSpace(request.EvidenceType)
                ? null
                : request.EvidenceType.Trim();
        }

        userSkill.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(userSkill));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserSkill(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "User skill was not found." });
        }

        try
        {
            dbContext.UserSkills.Remove(userSkill);
            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Cannot delete this user skill because it is being referenced by other records." });
        }
    }

    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = UserRoles.AcademicCounselor + "," + UserRoles.IndustryMentor)]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSkillResponse>> VerifyUserSkill(Guid id, CancellationToken cancellationToken)
    {
        var verifierId = GetCurrentUserId(); //lay id cua nguoi xac nhan
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken); //tim ky nang co id khop voi id trong request

        if (userSkill is null)
        {
            return NotFound(new { message = "User skill was not found." });
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.IsVerified = true;
        userSkill.VerifiedByUserId = verifierId; //gan id cua nguoi xac nhan vao
        userSkill.VerifiedAt = now;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(userSkill));
    }

    private static string? ValidateCreateRequest(CreateUserSkillRequest request)
    {
        if (request.SkillId == Guid.Empty)
        {
            return "Skill is required.";
        }

        var levelError = ValidateLevel(request.Level);
        if (levelError is not null)
        {
            return levelError;
        }

        return ValidateEvidenceFields(request.EvidenceUrl, request.EvidenceType);
    }

    private static string? ValidateEvidenceFields(string? evidenceUrl, string? evidenceType)
    {
        if (evidenceUrl is { Length: > 1024 })
        {
            return "Evidence URL must be at most 1024 characters.";
        }

        if (evidenceType is { Length: > 50 })
        {
            return "Evidence type must be at most 50 characters.";
        }

        return null;
    }

    private static string? ValidateLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return "Level is required.";
        }

        if (!AllowedLevels.Contains(level.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Level must be one of: Beginner, Intermediate, Advanced, Verified.";
        }

        return null;
    }

    private static string NormalizeLevel(string level) =>
        AllowedLevels.Single(value => value.Equals(level.Trim(), StringComparison.OrdinalIgnoreCase));

    private static UserSkillResponse ToResponse(UserSkill userSkill) =>
        new(
            userSkill.Id,
            userSkill.SkillId,
            userSkill.Skill.Name,
            userSkill.Skill.Category,
            userSkill.Level,
            userSkill.EvidenceUrl,
            userSkill.EvidenceType,
            userSkill.IsVerified,
            userSkill.VerifiedAt,
            userSkill.CreatedAt,
            userSkill.UpdatedAt);

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }
}
