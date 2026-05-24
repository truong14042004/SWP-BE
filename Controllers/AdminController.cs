using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;
using Microsoft.Extensions.Options;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin")]
public sealed class AdminController(
    AppDbContext dbContext,
    IFileStorageService storageService,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    private static readonly HashSet<string> LearningResourceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/plain",
        "text/markdown",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/zip",
        "application/x-zip-compressed"
    };

    private readonly StorageOptions options = storageOptions.Value;

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
            ? NotFound(new { message = "Không tìm thấy kỹ năng." })
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
            return Conflict(new { message = "Kỹ năng với tên và danh mục tương tự đã tồn tại." });
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
            return NotFound(new { message = "Không tìm thấy kỹ năng." });
        }

        var name = request.Name!.Trim();
        var category = request.Category!.Trim();
        var duplicate = await dbContext.Skills.AnyAsync(
            item => item.Id != id && item.Name == name && item.Category == category,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Kỹ năng với tên và danh mục tương tự đã tồn tại." });
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
            return NotFound(new { message = "Không tìm thấy kỹ năng." });
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
            ? NotFound(new { message = "Không tìm thấy tài nguyên học tập." })
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
            StorageObjectName = null,
            ContentType = null,
            FileSize = null,
            ResourceType = request.ResourceType!.Trim(),
            Difficulty = request.Difficulty?.Trim(),
            EstimatedHours = request.EstimatedHours,
            LessonNumber = request.LessonNumber ?? 1,
            IsActive = request.IsActive ?? true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.LearningResources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(resource).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetLearningResource), new { id = resource.Id }, ToResponse(resource));
    }

    [HttpPost("learning-resources/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LearningResourceResponse>> UploadLearningResource(
        [FromForm] UploadLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateUploadLearningResourceRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var resourceId = Guid.NewGuid();
        var objectName = BuildLearningResourceObjectName(resourceId, request.File.FileName, request.File.ContentType);

        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        var resource = new LearningResource
        {
            Id = resourceId,
            SkillId = request.SkillId,
            Title = request.Title!.Trim(),
            Url = $"/api/storage/learning-resources/{resourceId}/download",
            StorageObjectName = result.ObjectName,
            ContentType = result.ContentType,
            FileSize = result.Size,
            ResourceType = request.ResourceType!.Trim(),
            Difficulty = request.Difficulty?.Trim(),
            EstimatedHours = request.EstimatedHours,
            LessonNumber = request.LessonNumber ?? 1,
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
            return NotFound(new { message = "Không tìm thấy tài nguyên học tập." });
        }

        if (!string.IsNullOrWhiteSpace(resource.StorageObjectName))
        {
            await storageService.DeleteAsync(resource.StorageObjectName, cancellationToken);
        }

        resource.SkillId = request.SkillId;
        resource.Title = request.Title!.Trim();
        resource.Url = request.Url!.Trim();
        resource.StorageObjectName = null;
        resource.ContentType = null;
        resource.FileSize = null;
        resource.ResourceType = request.ResourceType!.Trim();
        resource.Difficulty = request.Difficulty?.Trim();
        resource.EstimatedHours = request.EstimatedHours;
        resource.LessonNumber = request.LessonNumber ?? 1;
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
            return NotFound(new { message = "Không tìm thấy tài nguyên học tập." });
        }

        if (!string.IsNullOrWhiteSpace(resource.StorageObjectName))
        {
            await storageService.DeleteAsync(resource.StorageObjectName, cancellationToken);
        }

        dbContext.LearningResources.Remove(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<string?> ValidateUploadLearningResourceRequest(
        UploadLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Tiêu đề tài nguyên học tập là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return "Loại tài nguyên học tập là bắt buộc.";
        }

        if (request.EstimatedHours is < 0)
        {
            return "Thời gian ước tính phải lớn hơn hoặc bằng 0.";
        }

        if (request.LessonNumber is < 1)
        {
            return "Số thứ tự bài học (Lesson Number) phải lớn hơn hoặc bằng 1.";
        }

        if (request.File is null || request.File.Length == 0)
        {
            return "Tệp tài nguyên học tập là bắt buộc.";
        }

        if (request.File.Length > options.MaxUploadBytes)
        {
            return $"Tệp quá lớn. Kích thước tối đa là {options.MaxUploadBytes} bytes.";
        }

        if (!LearningResourceContentTypes.Contains(request.File.ContentType))
        {
            return $"Định dạng tệp không được hỗ trợ: {request.File.ContentType}.";
        }

        if (request.SkillId is not null)
        {
            var skillExists = await dbContext.Skills.AnyAsync(
                skill => skill.Id == request.SkillId && skill.IsActive,
                cancellationToken);
            if (!skillExists)
            {
                return "Không tìm thấy kỹ năng đang hoạt động.";
            }
        }

        return null;
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
            ? NotFound(new { message = "Không tìm thấy yêu cầu kỹ năng của định hướng." })
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
            return Conflict(new { message = "Định hướng nghề nghiệp này đã có yêu cầu cho kỹ năng được chọn." });
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
            return NotFound(new { message = "Không tìm thấy yêu cầu kỹ năng của định hướng." });
        }

        var duplicate = await dbContext.RoleSkillRequirements.AnyAsync(
            item => item.Id != id
                && item.CareerRoleId == request.CareerRoleId
                && item.SkillId == request.SkillId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Định hướng nghề nghiệp này đã có yêu cầu cho kỹ năng được chọn." });
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
            return NotFound(new { message = "Không tìm thấy yêu cầu kỹ năng của định hướng." });
        }

        dbContext.RoleSkillRequirements.Remove(requirement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidateSkillRequest(SaveSkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Tên kỹ năng là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return "Danh mục kỹ năng là bắt buộc.";
        }

        return null;
    }

    private async Task<string?> ValidateLearningResourceRequest(
        SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Tiêu đề tài nguyên học tập là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return "Đường dẫn (URL) tài nguyên học tập là bắt buộc.";
        }

        if (!Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out _))
        {
            return "Đường dẫn (URL) tài nguyên học tập phải là đường dẫn tuyệt đối.";
        }

        if (string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return "Loại tài nguyên học tập là bắt buộc.";
        }

        if (request.EstimatedHours is < 0)
        {
            return "Thời gian ước tính phải lớn hơn hoặc bằng 0.";
        }

        if (request.LessonNumber is < 1)
        {
            return "Số thứ tự bài học (Lesson Number) phải lớn hơn hoặc bằng 1.";
        }

        if (request.SkillId is not null)
        {
            var skillExists = await dbContext.Skills.AnyAsync(
                skill => skill.Id == request.SkillId && skill.IsActive,
                cancellationToken);
            if (!skillExists)
            {
                return "Không tìm thấy kỹ năng đang hoạt động.";
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
            return "Định hướng nghề nghiệp là bắt buộc.";
        }

        if (request.SkillId == Guid.Empty)
        {
            return "Kỹ năng là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.RequiredLevel))
        {
            return "Yêu cầu cấp độ là bắt buộc.";
        }

        if (request.Priority is < 1 or > 5)
        {
            return "Độ ưu tiên phải từ 1 đến 5.";
        }

        if (request.Weight is <= 0)
        {
            return "Trọng số phải lớn hơn 0.";
        }

        var careerRoleExists = await dbContext.CareerRoles.AnyAsync(
            role => role.Id == request.CareerRoleId && role.IsActive,
            cancellationToken);
        if (!careerRoleExists)
        {
            return "Không tìm thấy định hướng nghề nghiệp đang hoạt động.";
        }

        var skillExists = await dbContext.Skills.AnyAsync(
            skill => skill.Id == request.SkillId && skill.IsActive,
            cancellationToken);
        if (!skillExists)
        {
            return "Không tìm thấy kỹ năng đang hoạt động.";
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
            resource.StorageObjectName is null ? "Link" : "File",
            resource.ContentType,
            resource.FileSize,
            resource.ResourceType,
            resource.Difficulty,
            resource.EstimatedHours,
            resource.LessonNumber,
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

    private static string BuildLearningResourceObjectName(
        Guid resourceId,
        string? originalFileName,
        string? contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GetExtension(contentType);
        }

        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        baseName = string.IsNullOrWhiteSpace(baseName)
            ? "resource"
            : new string(baseName
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "resource";
        }

        extension = !string.IsNullOrWhiteSpace(extension)
            && extension.Length <= 11
            && extension[0] == '.'
            && extension.Skip(1).All(char.IsLetterOrDigit)
                ? extension.ToLowerInvariant()
                : string.Empty;

        return $"learning-resources/{resourceId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{baseName}{extension}";
    }

    private static string GetExtension(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "text/markdown" => ".md",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-powerpoint" => ".ppt",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/zip" => ".zip",
            "application/x-zip-compressed" => ".zip",
            _ => string.Empty
        };
    }
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
    bool? IsActive,
    int? LessonNumber);

public sealed class UploadLearningResourceRequest
{
    public Guid? SkillId { get; set; }
    public string? Title { get; set; }
    public string? ResourceType { get; set; }
    public string? Difficulty { get; set; }
    public int? EstimatedHours { get; set; }
    public bool? IsActive { get; set; }
    public int? LessonNumber { get; set; }
    public IFormFile File { get; set; } = null!;
}

public sealed record LearningResourceResponse(
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
    int LessonNumber,
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
