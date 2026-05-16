using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin")]
public sealed class AdminController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("skills")]
    public async Task<ActionResult<IReadOnlyList<SkillResponse>>> GetSkills(CancellationToken cancellationToken)
    {
        var skills = await dbContext.Skills
            .AsNoTracking()
            .OrderBy(skill => skill.Category)
            .ThenBy(skill => skill.Name)
            .Select(skill => ToResponse(skill))
            .ToListAsync(cancellationToken);

        return Ok(skills);
    }

    [HttpGet("skills/{id:guid}")]
    public async Task<ActionResult<SkillResponse>> GetSkill(Guid id, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return skill is null
            ? NotFound(new { message = "Skill was not found." })
            : Ok(ToResponse(skill));
    }

    [HttpPost("skills")]
    public async Task<ActionResult<SkillResponse>> CreateSkill(
        SaveSkillRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateSkillRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var name = request.Name!.Trim();
        var category = request.Category!.Trim();
        var exists = await dbContext.Skills.AnyAsync(
            skill => skill.Name == name && skill.Category == category,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "A skill with the same name and category already exists." });
        }

        var now = DateTimeOffset.UtcNow;
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive ?? true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Skills.Add(skill);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, ToResponse(skill));
    }

    [HttpPut("skills/{id:guid}")]
    public async Task<ActionResult<SkillResponse>> UpdateSkill(
        Guid id,
        SaveSkillRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateSkillRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var skill = await dbContext.Skills.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (skill is null)
        {
            return NotFound(new { message = "Skill was not found." });
        }

        var name = request.Name!.Trim();
        var category = request.Category!.Trim();
        var duplicate = await dbContext.Skills.AnyAsync(
            item => item.Id != id && item.Name == name && item.Category == category,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "A skill with the same name and category already exists." });
        }

        skill.Name = name;
        skill.Category = category;
        skill.Description = request.Description?.Trim();
        skill.IsActive = request.IsActive ?? skill.IsActive;
        skill.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(skill));
    }

    [HttpDelete("skills/{id:guid}")]
    public async Task<IActionResult> DeleteSkill(Guid id, CancellationToken cancellationToken)
    {
        var skill = await dbContext.Skills.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (skill is null)
        {
            return NotFound(new { message = "Skill was not found." });
        }

        var isUsed = await dbContext.UserSkills.AnyAsync(item => item.SkillId == id, cancellationToken)
            || await dbContext.RoleSkillRequirements.AnyAsync(item => item.SkillId == id, cancellationToken)
            || await dbContext.GithubRepositorySkills.AnyAsync(item => item.SkillId == id, cancellationToken)
            || await dbContext.SkillGapReportItems.AnyAsync(item => item.SkillId == id, cancellationToken)
            || await dbContext.RoadmapNodes.AnyAsync(item => item.SkillId == id, cancellationToken);

        if (isUsed)
        {
            skill.IsActive = false;
            skill.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            dbContext.Skills.Remove(skill);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("learning-resources")]
    public async Task<ActionResult<IReadOnlyList<LearningResourceResponse>>> GetLearningResources(
        CancellationToken cancellationToken)
    {
        var resources = await dbContext.LearningResources
            .AsNoTracking()
            .Include(resource => resource.Skill)
            .OrderBy(resource => resource.Skill == null ? null : resource.Skill.Category)
            .ThenBy(resource => resource.Skill == null ? null : resource.Skill.Name)
            .ThenBy(resource => resource.Title)
            .Select(resource => ToResponse(resource))
            .ToListAsync(cancellationToken);

        return Ok(resources);
    }

    [HttpGet("learning-resources/{id:guid}")]
    public async Task<ActionResult<LearningResourceResponse>> GetLearningResource(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.LearningResources
            .AsNoTracking()
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return resource is null
            ? NotFound(new { message = "Learning resource was not found." })
            : Ok(ToResponse(resource));
    }

    [HttpPost("learning-resources")]
    public async Task<ActionResult<LearningResourceResponse>> CreateLearningResource(
        SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateLearningResourceRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var resource = new LearningResource
        {
            Id = Guid.NewGuid(),
            SkillId = request.SkillId,
            Title = request.Title!.Trim(),
            Url = request.Url!.Trim(),
            ResourceType = request.ResourceType!.Trim(),
            Difficulty = request.Difficulty?.Trim(),
            EstimatedHours = request.EstimatedHours,
            IsActive = request.IsActive ?? true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.LearningResources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(resource).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetLearningResource), new { id = resource.Id }, ToResponse(resource));
    }

    [HttpPut("learning-resources/{id:guid}")]
    public async Task<ActionResult<LearningResourceResponse>> UpdateLearningResource(
        Guid id,
        SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateLearningResourceRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var resource = await dbContext.LearningResources
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        resource.SkillId = request.SkillId;
        resource.Title = request.Title!.Trim();
        resource.Url = request.Url!.Trim();
        resource.ResourceType = request.ResourceType!.Trim();
        resource.Difficulty = request.Difficulty?.Trim();
        resource.EstimatedHours = request.EstimatedHours;
        resource.IsActive = request.IsActive ?? resource.IsActive;
        resource.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(resource).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return Ok(ToResponse(resource));
    }

    [HttpDelete("learning-resources/{id:guid}")]
    public async Task<IActionResult> DeleteLearningResource(Guid id, CancellationToken cancellationToken)
    {
        var resource = await dbContext.LearningResources.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return NotFound(new { message = "Learning resource was not found." });
        }

        dbContext.LearningResources.Remove(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("role-skill-requirements")]
    public async Task<ActionResult<IReadOnlyList<RoleSkillRequirementResponse>>> GetRoleSkillRequirements(
        Guid? careerRoleId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Include(requirement => requirement.CareerRole)
            .Include(requirement => requirement.Skill)
            .AsQueryable();

        if (careerRoleId is not null)
        {
            query = query.Where(requirement => requirement.CareerRoleId == careerRoleId);
        }

        var requirements = await query
            .OrderBy(requirement => requirement.CareerRole.Name)
            .ThenBy(requirement => requirement.Priority)
            .ThenBy(requirement => requirement.Skill.Name)
            .Select(requirement => ToResponse(requirement))
            .ToListAsync(cancellationToken);

        return Ok(requirements);
    }

    [HttpGet("role-skill-requirements/{id:guid}")]
    public async Task<ActionResult<RoleSkillRequirementResponse>> GetRoleSkillRequirement(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requirement = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Include(item => item.CareerRole)
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return requirement is null
            ? NotFound(new { message = "Role skill requirement was not found." })
            : Ok(ToResponse(requirement));
    }

    [HttpPost("role-skill-requirements")]
    public async Task<ActionResult<RoleSkillRequirementResponse>> CreateRoleSkillRequirement(
        SaveRoleSkillRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRoleSkillRequirementRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var duplicate = await dbContext.RoleSkillRequirements.AnyAsync(
            item => item.CareerRoleId == request.CareerRoleId && item.SkillId == request.SkillId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "This career role already has a requirement for the selected skill." });
        }

        var now = DateTimeOffset.UtcNow;
        var requirement = new RoleSkillRequirement
        {
            Id = Guid.NewGuid(),
            CareerRoleId = request.CareerRoleId,
            SkillId = request.SkillId,
            RequiredLevel = request.RequiredLevel!.Trim(),
            Priority = request.Priority,
            Weight = request.Weight ?? 1m,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.RoleSkillRequirements.Add(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.CareerRole).LoadAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoleSkillRequirement), new { id = requirement.Id }, ToResponse(requirement));
    }

    [HttpPut("role-skill-requirements/{id:guid}")]
    public async Task<ActionResult<RoleSkillRequirementResponse>> UpdateRoleSkillRequirement(
        Guid id,
        SaveRoleSkillRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRoleSkillRequirementRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var requirement = await dbContext.RoleSkillRequirements
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requirement is null)
        {
            return NotFound(new { message = "Role skill requirement was not found." });
        }

        var duplicate = await dbContext.RoleSkillRequirements.AnyAsync(
            item => item.Id != id
                && item.CareerRoleId == request.CareerRoleId
                && item.SkillId == request.SkillId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "This career role already has a requirement for the selected skill." });
        }

        requirement.CareerRoleId = request.CareerRoleId;
        requirement.SkillId = request.SkillId;
        requirement.RequiredLevel = request.RequiredLevel!.Trim();
        requirement.Priority = request.Priority;
        requirement.Weight = request.Weight ?? requirement.Weight;
        requirement.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.CareerRole).LoadAsync(cancellationToken);
        await dbContext.Entry(requirement).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return Ok(ToResponse(requirement));
    }

    [HttpDelete("role-skill-requirements/{id:guid}")]
    public async Task<IActionResult> DeleteRoleSkillRequirement(Guid id, CancellationToken cancellationToken)
    {
        var requirement = await dbContext.RoleSkillRequirements.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requirement is null)
        {
            return NotFound(new { message = "Role skill requirement was not found." });
        }

        dbContext.RoleSkillRequirements.Remove(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidateSkillRequest(SaveSkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Skill name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return "Skill category is required.";
        }

        return null;
    }

    private async Task<string?> ValidateLearningResourceRequest(
        SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Learning resource title is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return "Learning resource URL is required.";
        }

        if (!Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out _))
        {
            return "Learning resource URL must be absolute.";
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return "Learning resource type is required.";
        }

        if (request.EstimatedHours is < 0)
        {
            return "Estimated hours must be greater than or equal to 0.";
        }

        if (request.SkillId is not null)
        {
            var skillExists = await dbContext.Skills.AnyAsync(
                skill => skill.Id == request.SkillId && skill.IsActive,
                cancellationToken);
            if (!skillExists)
            {
                return "Active skill was not found.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateRoleSkillRequirementRequest(
        SaveRoleSkillRequirementRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CareerRoleId == Guid.Empty)
        {
            return "Career role is required.";
        }

        if (request.SkillId == Guid.Empty)
        {
            return "Skill is required.";
        }

        if (string.IsNullOrWhiteSpace(request.RequiredLevel))
        {
            return "Required level is required.";
        }

        if (request.Priority is < 1 or > 5)
        {
            return "Priority must be from 1 to 5.";
        }

        if (request.Weight is <= 0)
        {
            return "Weight must be greater than 0.";
        }

        var careerRoleExists = await dbContext.CareerRoles.AnyAsync(
            role => role.Id == request.CareerRoleId && role.IsActive,
            cancellationToken);
        if (!careerRoleExists)
        {
            return "Active career role was not found.";
        }

        var skillExists = await dbContext.Skills.AnyAsync(
            skill => skill.Id == request.SkillId && skill.IsActive,
            cancellationToken);
        if (!skillExists)
        {
            return "Active skill was not found.";
        }

        return null;
    }

    private static SkillResponse ToResponse(Skill skill) =>
        new(
            skill.Id,
            skill.Name,
            skill.Category,
            skill.Description,
            skill.IsActive,
            skill.CreatedAt,
            skill.UpdatedAt);

    private static LearningResourceResponse ToResponse(LearningResource resource) =>
        new(
            resource.Id,
            resource.SkillId,
            resource.Skill?.Name,
            resource.Title,
            resource.Url,
            resource.ResourceType,
            resource.Difficulty,
            resource.EstimatedHours,
            resource.IsActive,
            resource.CreatedAt,
            resource.UpdatedAt);

    private static RoleSkillRequirementResponse ToResponse(RoleSkillRequirement requirement) =>
        new(
            requirement.Id,
            requirement.CareerRoleId,
            requirement.CareerRole.Name,
            requirement.SkillId,
            requirement.Skill.Name,
            requirement.RequiredLevel,
            requirement.Priority,
            requirement.Weight,
            requirement.CreatedAt,
            requirement.UpdatedAt);
}

public sealed record SaveSkillRequest(
    string? Name,
    string? Category,
    string? Description,
    bool? IsActive);

public sealed record SkillResponse(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveLearningResourceRequest(
    Guid? SkillId,
    string? Title,
    string? Url,
    string? ResourceType,
    string? Difficulty,
    int? EstimatedHours,
    bool? IsActive);

public sealed record LearningResourceResponse(
    Guid Id,
    Guid? SkillId,
    string? SkillName,
    string Title,
    string Url,
    string ResourceType,
    string? Difficulty,
    int? EstimatedHours,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveRoleSkillRequirementRequest(
    Guid CareerRoleId,
    Guid SkillId,
    string? RequiredLevel,
    int Priority,
    decimal? Weight);

public sealed record RoleSkillRequirementResponse(
    Guid Id,
    Guid CareerRoleId,
    string CareerRoleName,
    Guid SkillId,
    string SkillName,
    string RequiredLevel,
    int Priority,
    decimal Weight,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
