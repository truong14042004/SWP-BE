using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/industry-mentor")] //Industry Mentor review portfolio sinh vien da publish
[Authorize(Roles = UserRoles.IndustryMentor)]
public sealed class IndustryMentorController(
    AppDbContext dbContext,
    IStudentReviewQuotaService quotaService,
    INotificationService notificationService,
    IFileStorageService storageService,
    IOptions<StorageOptions> storageOptions,
    IAutoEvolveAiService autoEvolveAiService,
    ILogger<IndustryMentorController> logger) : ControllerBase
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

    private readonly StorageOptions storageOpts = storageOptions.Value;

    private static readonly string[] AllowedJobReadinessLevels =
    [
        "NotReady",
        "NeedsImprovement",
        "Ready",
        "Excellent"
    ];

    [HttpGet("review-queue")]
    public async Task<ActionResult<IReadOnlyList<MentorStudentSummaryResponse>>> GetReviewQueue(
        CancellationToken cancellationToken)
    {
        var responses = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRoles.Student && user.IsActive)
            .Select(user => new //tao anonymous object chua user va portfolio da publish
            {
                User = user,
                Portfolio = dbContext.Portfolios
                    .Where(portfolio => portfolio.UserId == user.Id && portfolio.IsPublished)
                    .OrderByDescending(portfolio => portfolio.CreatedAt)
                    .FirstOrDefault(),
                Profile = dbContext.StudentProfiles
                    .Where(profile => profile.UserId == user.Id)
                    .FirstOrDefault()
            })
            .Where(item => item.Portfolio != null)
            .OrderByDescending(item => item.Portfolio!.PublishedAt ?? item.Portfolio.CreatedAt)
            .Select(item => new MentorStudentSummaryResponse(
                item.User.Id,
                item.User.FullName,
                item.User.Email,
                item.User.Username,
                item.User.AvatarUrl,
                item.User.CreatedAt,
                item.Portfolio!.Slug,
                item.Portfolio.Title,
                item.Portfolio.PublishedAt,
                item.Profile != null ? item.Profile.CvUrl : null,
                item.Profile != null ? item.Profile.CvName : null))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    [HttpGet("students/{studentId:guid}/portfolio")] //xem chi tiet portfolio cua sinh vien
    public async Task<ActionResult<PortfolioResponse>> GetStudentPortfolio(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
            .Where(item => item.UserId == studentId && item.IsPublished)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Không tìm thấy Portfolio." });
        }

        return Ok(await ToPortfolioResponse(portfolio, cancellationToken));
    }

    [HttpGet("students/{studentId:guid}/github")] //lay danh sach repo cua sinh vien
    public async Task<ActionResult<IReadOnlyList<MentorGithubRepoResponse>>> GetStudentGithubRepositories(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var responses = await dbContext.GithubRepositories
            .AsNoTracking()
            .Where(item => item.UserId == studentId)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new MentorGithubRepoResponse(
                item.Id,
                item.RepoName,
                item.RepoUrl,
                item.Description,
                item.AiSummary,
                item.TechStackJson,
                item.QualityScore,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    [HttpGet("students/{studentId:guid}/quota")] //mentor xem con bao nhieu luot review cho sinh vien
    public async Task<ActionResult<MentorReviewQuotaResponse>> GetReviewQuota(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var quota = await quotaService.GetQuotaAsync(studentId, cancellationToken);
        return Ok(ToResponse(quota));
    }

    // ========== Skill verification (C5) ==========

    [HttpGet("students/{studentId:guid}/skills")] //mentor xem ky nang cua sinh vien de xac minh
    public async Task<ActionResult<IReadOnlyList<MentorViewableUserSkillResponse>>> GetStudentSkills(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var skills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(item => item.Skill)
            .Where(item => item.UserId == studentId)
            .OrderBy(item => item.Skill.Category)
            .ThenBy(item => item.Skill.Name)
            .ToListAsync(cancellationToken);

        if (skills.Count == 0)
        {
            return Ok(Array.Empty<MentorViewableUserSkillResponse>());
        }

        // Resolve verifier names
        var verifierIds = skills
            .Where(item => item.VerifiedByUserId.HasValue)
            .Select(item => item.VerifiedByUserId!.Value)
            .Distinct()
            .ToList();

        var verifiers = verifierIds.Count == 0
            ? new Dictionary<Guid, (string FullName, string Role)>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(item => verifierIds.Contains(item.Id))
                .Select(item => new { item.Id, item.FullName, item.Role })
                .ToDictionaryAsync(item => item.Id, item => (item.FullName, item.Role), cancellationToken);

        var responses = skills
            .Select(item =>
            {
                string? verifierName = null;
                string? verifierRole = null;
                if (item.VerifiedByUserId.HasValue
                    && verifiers.TryGetValue(item.VerifiedByUserId.Value, out var verifier))
                {
                    verifierName = verifier.FullName;
                    verifierRole = verifier.Role;
                }

                return new MentorViewableUserSkillResponse(
                    item.Id,
                    item.SkillId,
                    item.Skill.Name,
                    item.Skill.Category,
                    item.Level,
                    item.EvidenceUrl,
                    item.EvidenceType,
                    item.IsVerified,
                    item.VerifiedAt,
                    item.VerifiedByUserId,
                    verifierName,
                    verifierRole,
                    item.CreatedAt,
                    item.UpdatedAt);
            })
            .ToList();

        return Ok(responses);
    }

    [HttpPost("user-skills/{userSkillId:guid}/verify")] //mentor xac minh ky nang cua sinh vien
    public async Task<ActionResult<MentorViewableUserSkillResponse>> VerifyStudentSkill(
        Guid userSkillId,
        CancellationToken cancellationToken)
    {
        var mentorId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == userSkillId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng đã được xác minh." });
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.IsVerified = true;
        userSkill.VerifiedByUserId = mentorId;
        userSkill.VerifiedAt = now;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var mentor = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == mentorId)
            .Select(item => new { item.FullName, item.Role })
            .SingleAsync(cancellationToken);

        return Ok(new MentorViewableUserSkillResponse(
            userSkill.Id,
            userSkill.SkillId,
            userSkill.Skill.Name,
            userSkill.Skill.Category,
            userSkill.Level,
            userSkill.EvidenceUrl,
            userSkill.EvidenceType,
            userSkill.IsVerified,
            userSkill.VerifiedAt,
            userSkill.VerifiedByUserId,
            mentor.FullName,
            mentor.Role,
            userSkill.CreatedAt,
            userSkill.UpdatedAt));
    }

    [HttpPost("user-skills/{userSkillId:guid}/unverify")] //mentor rut lai xac minh (chi nguoi verify ban dau)
    public async Task<ActionResult<MentorViewableUserSkillResponse>> UnverifyStudentSkill(
        Guid userSkillId,
        CancellationToken cancellationToken)
    {
        var mentorId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == userSkillId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (!userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng hiện chưa được xác minh." });
        }

        // Chi nguoi verify ban dau moi rut lai duoc
        if (userSkill.VerifiedByUserId != mentorId)
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.IsVerified = false;
        userSkill.VerifiedByUserId = null;
        userSkill.VerifiedAt = null;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MentorViewableUserSkillResponse(
            userSkill.Id,
            userSkill.SkillId,
            userSkill.Skill.Name,
            userSkill.Skill.Category,
            userSkill.Level,
            userSkill.EvidenceUrl,
            userSkill.EvidenceType,
            userSkill.IsVerified,
            userSkill.VerifiedAt,
            userSkill.VerifiedByUserId,
            null,
            null,
            userSkill.CreatedAt,
            userSkill.UpdatedAt));
    }

    [HttpPost("feedback")] //mentor tao feedback cho sinh vien
    public async Task<ActionResult<MentorFeedbackResponse>> CreateFeedback(
        CreateMentorFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { message = "Nhận xét là bắt buộc." });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Đánh giá phải từ 1 đến 5 sao." });
        }

        if (!string.IsNullOrWhiteSpace(request.JobReadinessLevel)
            && !AllowedJobReadinessLevels.Contains(request.JobReadinessLevel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = $"Mức độ sẵn sàng làm việc phải thuộc một trong các giá trị: {string.Join(", ", AllowedJobReadinessLevels)}."
            });
        }

        var mentorId = GetCurrentUserId();
        var student = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.StudentId && item.Role == UserRoles.Student && item.IsActive,
                cancellationToken);
        if (student is null)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
        }

        if (request.PortfolioId is not null) //kiem tra portfolio thuoc ve sinh vien
        {
            var hasPortfolio = await dbContext.Portfolios.AnyAsync(
                item => item.Id == request.PortfolioId && item.UserId == request.StudentId,
                cancellationToken);
            if (!hasPortfolio)
            {
                return BadRequest(new { message = "Portfolio không thuộc về sinh viên." });
            }
        }

        if (request.GithubRepositoryId is not null) //kiem tra repo thuoc ve sinh vien
        {
            var hasRepository = await dbContext.GithubRepositories.AnyAsync(
                item => item.Id == request.GithubRepositoryId && item.UserId == request.StudentId,
                cancellationToken);
            if (!hasRepository)
            {
                return BadRequest(new { message = "Kho lưu trữ GitHub không thuộc về sinh viên." });
            }
        }

        //task 3: tru mentor review usage theo subscription plan cua sinh vien
        var quota = await quotaService.GetQuotaAsync(request.StudentId, cancellationToken);
        if (quota.Remaining <= 0)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, new
            {
                message = $"Sinh viên đã hết lượt mentor review ({quota.Used}/{quota.Limit}). Vui lòng nâng cấp gói để tiếp tục.",
                quota = ToResponse(quota)
            });
        }

        var now = DateTimeOffset.UtcNow;
        var feedback = new MentorFeedback
        {
            Id = Guid.NewGuid(),
            MentorId = mentorId,
            StudentId = request.StudentId,
            PortfolioId = request.PortfolioId,
            GithubRepositoryId = request.GithubRepositoryId,
            Comment = request.Comment.Trim(),
            Rating = request.Rating,
            PortfolioQualityFeedback = NormalizeOptional(request.PortfolioQualityFeedback),
            TechnicalSkillsAssessment = NormalizeOptional(request.TechnicalSkillsAssessment),
            ProjectQualityFeedback = NormalizeOptional(request.ProjectQualityFeedback),
            Recommendations = NormalizeOptional(request.Recommendations),
            JobReadinessLevel = NormalizeJobReadinessLevel(request.JobReadinessLevel),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.MentorFeedbacks.Add(feedback);

        var mentor = await dbContext.Users //lay mentor name de luu vo response
            .AsNoTracking()
            .SingleAsync(item => item.Id == mentorId, cancellationToken);

        // H2: thong bao cho sinh vien khi nhan feedback portfolio tu mentor
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.SendNotificationAsync(
            request.StudentId,
            "PortfolioFeedbackReceived",
            "Bạn nhận được feedback mới",
            $"{mentor.FullName} đã gửi feedback portfolio cho bạn.",
            "#portfolio",
            JsonSerializer.Serialize(new
            {
                feedbackId = feedback.Id,
                mentorId,
                mentorName = mentor.FullName,
                rating = feedback.Rating,
                jobReadinessLevel = feedback.JobReadinessLevel,
            }),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ToMentorFeedbackResponse(feedback, mentor.FullName, student.FullName));
    }

    [HttpGet("feedback")] //lay danh sach feedback cua mentor
    public async Task<ActionResult<IReadOnlyList<MentorFeedbackResponse>>> GetMyFeedback(
        CancellationToken cancellationToken)
    {
        var mentorId = GetCurrentUserId();
        var responses = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .Include(item => item.Mentor)
            .Include(item => item.Student)
            .Where(item => item.MentorId == mentorId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new MentorFeedbackResponse(
                item.Id,
                item.MentorId,
                item.Mentor.FullName,
                item.StudentId,
                item.Student.FullName,
                item.PortfolioId,
                item.GithubRepositoryId,
                item.Comment,
                item.Rating,
                item.PortfolioQualityFeedback,
                item.TechnicalSkillsAssessment,
                item.ProjectQualityFeedback,
                item.Recommendations,
                item.JobReadinessLevel,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    [HttpGet("students/{studentId:guid}/feedback")] //lay danh sach feedback cua mentor theo tung sinh vien
    public async Task<ActionResult<IReadOnlyList<MentorFeedbackResponse>>> GetStudentFeedbackByMentor(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var mentorId = GetCurrentUserId();
        var responses = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .Include(item => item.Mentor)
            .Include(item => item.Student)
            .Where(item => item.MentorId == mentorId && item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new MentorFeedbackResponse(
                item.Id,
                item.MentorId,
                item.Mentor.FullName,
                item.StudentId,
                item.Student.FullName,
                item.PortfolioId,
                item.GithubRepositoryId,
                item.Comment,
                item.Rating,
                item.PortfolioQualityFeedback,
                item.TechnicalSkillsAssessment,
                item.ProjectQualityFeedback,
                item.Recommendations,
                item.JobReadinessLevel,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    // ========== Skills, learning resources, role requirements (admin) ==========

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
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LearningResourceResponse>> CreateLearningResource(
        [FromForm] SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateLearningResourceRequest(request, hasExistingFile: false, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTimeOffset.UtcNow;
        var resourceId = Guid.NewGuid();

        LearningResource resource;
        if (request.File is not null && request.File.Length > 0)
        {
            var objectName = BuildLearningResourceObjectName(resourceId, request.File.FileName, request.File.ContentType);
            await using var stream = request.File.OpenReadStream();
            var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

            resource = new LearningResource
            {
                Id = resourceId,
                SkillId = request.SkillId,
                Title = request.Title!.Trim(),
                Url = NormalizeExternalResourceUrl(request.Url) ?? string.Empty,
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
        }
        else
        {
            resource = new LearningResource
            {
                Id = resourceId,
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
        }

        dbContext.LearningResources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(resource).Reference(item => item.Skill).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetLearningResource), new { id = resource.Id }, ToResponse(resource));
    }

    [HttpPut("learning-resources/{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LearningResourceResponse>> UpdateLearningResource(
        Guid id,
        [FromForm] SaveLearningResourceRequest request,
        CancellationToken cancellationToken)
    {
        var resource = await dbContext.LearningResources
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null)
        {
            return NotFound(new { message = "Không tìm thấy tài nguyên học tập." });
        }

        var validationError = await ValidateLearningResourceRequest(
            request,
            hasExistingFile: !string.IsNullOrWhiteSpace(resource.StorageObjectName),
            cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        resource.SkillId = request.SkillId;
        resource.Title = request.Title!.Trim();
        resource.ResourceType = request.ResourceType!.Trim();
        resource.Difficulty = request.Difficulty?.Trim();
        resource.EstimatedHours = request.EstimatedHours;
        resource.LessonNumber = request.LessonNumber ?? 1;
        resource.IsActive = request.IsActive ?? resource.IsActive;
        resource.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.File is not null && request.File.Length > 0)
        {
            if (!string.IsNullOrWhiteSpace(resource.StorageObjectName))
            {
                await storageService.DeleteAsync(resource.StorageObjectName, cancellationToken);
            }

            var objectName = BuildLearningResourceObjectName(id, request.File.FileName, request.File.ContentType);
            await using var stream = request.File.OpenReadStream();
            var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

            resource.Url = NormalizeExternalResourceUrl(request.Url) ?? string.Empty;
            resource.StorageObjectName = result.ObjectName;
            resource.ContentType = result.ContentType;
            resource.FileSize = result.Size;
        }
        else
        {
            var trimmedUrl = request.Url?.Trim() ?? string.Empty;
            var isLocalDownloadUrl = IsInternalLearningResourceUrl(trimmedUrl);
            var hasExistingFile = !string.IsNullOrWhiteSpace(resource.StorageObjectName);

            if (string.IsNullOrWhiteSpace(trimmedUrl) || isLocalDownloadUrl)
            {
                resource.Url = string.Empty;
            }
            else
            {
                resource.Url = trimmedUrl;
                if (!hasExistingFile)
                {
                    resource.StorageObjectName = null;
                    resource.ContentType = null;
                    resource.FileSize = null;
                }
            }
        }

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

    [HttpGet("skill-prerequisites")]
    public async Task<ActionResult<IReadOnlyList<SkillPrerequisiteResponse>>> GetSkillPrerequisites(
        CancellationToken cancellationToken)
    {
        var prerequisites = await dbContext.SkillPrerequisites
            .AsNoTracking()
            .Include(item => item.Skill)
            .Include(item => item.PrerequisiteSkill)
            .OrderBy(item => item.Skill.Name)
            .ThenBy(item => item.PrerequisiteSkill.Name)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        return Ok(prerequisites);
    }

    [HttpGet("skill-prerequisites/{id:guid}")]
    public async Task<ActionResult<SkillPrerequisiteResponse>> GetSkillPrerequisite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var prerequisite = await dbContext.SkillPrerequisites
            .AsNoTracking()
            .Include(item => item.Skill)
            .Include(item => item.PrerequisiteSkill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return prerequisite is null
            ? NotFound(new { message = "Không tìm thấy quan hệ tiên quyết." })
            : Ok(ToResponse(prerequisite));
    }

    [HttpPost("skill-prerequisites")]
    public async Task<ActionResult<SkillPrerequisiteResponse>> CreateSkillPrerequisite(
        SaveSkillPrerequisiteRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateSkillPrerequisiteRequest(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var prerequisite = new SkillPrerequisite
        {
            Id = Guid.NewGuid(),
            SkillId = request.SkillId,
            PrerequisiteSkillId = request.PrerequisiteSkillId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.SkillPrerequisites.Add(prerequisite);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(prerequisite).Reference(item => item.Skill).LoadAsync(cancellationToken);
        await dbContext.Entry(prerequisite).Reference(item => item.PrerequisiteSkill).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSkillPrerequisite), new { id = prerequisite.Id }, ToResponse(prerequisite));
    }

    [HttpDelete("skill-prerequisites/{id:guid}")]
    public async Task<IActionResult> DeleteSkillPrerequisite(Guid id, CancellationToken cancellationToken)
    {
        var prerequisite = await dbContext.SkillPrerequisites.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (prerequisite is null)
        {
            return NotFound(new { message = "Không tìm thấy quan hệ tiên quyết." });
        }

        dbContext.SkillPrerequisites.Remove(prerequisite);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("career-roles")]
    public async Task<IActionResult> CreateCareerRole(
        CreateCareerRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên định hướng là bắt buộc." });
        }

        var exists = await dbContext.CareerRoles.AnyAsync(
            role => role.Name == request.Name.Trim(),
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Định hướng nghề nghiệp với tên này đã tồn tại." });
        }

        var now = DateTimeOffset.UtcNow;
        var role = new CareerRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Level = request.Level?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.CareerRoles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created(
            $"/api/career-roles/{role.Id}",
            new
            {
                role.Id,
                role.Name,
                role.Description,
                role.Level,
                role.IsActive,
                role.CreatedAt,
                role.UpdatedAt
            });
    }

    [HttpPut("career-roles/{id:guid}")]
    public async Task<IActionResult> UpdateCareerRole(
        Guid id,
        UpdateCareerRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên định hướng là bắt buộc." });
        }

        var role = await dbContext.CareerRoles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (role is null)
        {
            return NotFound(new { message = "Không tìm thấy định hướng nghề nghiệp." });
        }

        var exists = await dbContext.CareerRoles.AnyAsync(
            item => item.Name == request.Name.Trim() && item.Id != id,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Định hướng nghề nghiệp khác với tên này đã tồn tại." });
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();
        role.Level = request.Level?.Trim();
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            role.Id,
            role.Name,
            role.Description,
            role.Level,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt
        });
    }

    [HttpPost("auto-evolve/generate/{careerRoleId:guid}")]
    public async Task<IActionResult> GenerateProposals(Guid careerRoleId, CancellationToken cancellationToken)
    {
        try
        {
            await autoEvolveAiService.GenerateProposalsAsync(careerRoleId, cancellationToken);
            return Ok(new { message = "Đã sinh đề xuất thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi sinh đề xuất Auto-Evolve.");
            return StatusCode(500, new { message = "Lỗi hệ thống khi sinh đề xuất bằng AI." });
        }
    }

    [HttpGet("auto-evolve/proposals")]
    public async Task<IActionResult> GetPendingProposals(CancellationToken cancellationToken)
    {
        var proposals = await dbContext.RoleSkillUpdateProposals
            .AsNoTracking()
            .Include(p => p.CareerRole)
            .Where(p => p.Status == "Pending")
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                CareerRoleId = p.CareerRoleId,
                CareerRoleName = p.CareerRole.Name,
                SkillId = p.SkillId,
                SkillName = p.SkillName,
                ActionType = p.ActionType,
                CurrentPriority = p.CurrentPriority,
                ProposedPriority = p.ProposedPriority,
                CurrentWeight = p.CurrentWeight,
                ProposedWeight = p.ProposedWeight,
                Reason = p.Reason,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(proposals);
    }

    [HttpPost("auto-evolve/proposals/{id:guid}/approve")]
    public async Task<IActionResult> ApproveProposal(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await dbContext.RoleSkillUpdateProposals
            .Include(p => p.CareerRole)
            .SingleOrDefaultAsync(p => p.Id == id && p.Status == "Pending", cancellationToken);

        if (proposal is null)
        {
            return NotFound(new { message = "Không tìm thấy đề xuất chờ duyệt." });
        }

        if (proposal.ActionType == "UpdatePriority" || proposal.ActionType == "UpdateWeight")
        {
            var req = await dbContext.RoleSkillRequirements
                .SingleOrDefaultAsync(
                    r => r.CareerRoleId == proposal.CareerRoleId && r.SkillId == proposal.SkillId,
                    cancellationToken);

            if (req is null)
            {
                return BadRequest(new { message = "Không tìm thấy yêu cầu kỹ năng tương ứng để cập nhật." });
            }

            if (proposal.ActionType == "UpdatePriority" && proposal.ProposedPriority.HasValue)
            {
                req.Priority = proposal.ProposedPriority.Value;
            }
            else if (proposal.ActionType == "UpdateWeight" && proposal.ProposedWeight.HasValue)
            {
                req.Weight = proposal.ProposedWeight.Value;
            }

            req.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (proposal.ActionType == "AddSkill")
        {
            var exists = await dbContext.RoleSkillRequirements
                .AnyAsync(
                    r => r.CareerRoleId == proposal.CareerRoleId && r.SkillId == proposal.SkillId,
                    cancellationToken);

            if (!exists)
            {
                dbContext.RoleSkillRequirements.Add(new RoleSkillRequirement
                {
                    Id = Guid.NewGuid(),
                    CareerRoleId = proposal.CareerRoleId,
                    SkillId = proposal.SkillId,
                    RequiredLevel = "Intermediate",
                    Priority = proposal.ProposedPriority ?? 3,
                    Weight = proposal.ProposedWeight ?? 1m,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        proposal.Status = "Approved";
        proposal.RespondedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã duyệt đề xuất." });
    }

    [HttpPost("auto-evolve/proposals/{id:guid}/reject")]
    public async Task<IActionResult> RejectProposal(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await dbContext.RoleSkillUpdateProposals
            .SingleOrDefaultAsync(p => p.Id == id && p.Status == "Pending", cancellationToken);

        if (proposal is null)
        {
            return NotFound(new { message = "Không tìm thấy đề xuất chờ duyệt." });
        }

        proposal.Status = "Rejected";
        proposal.RespondedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã từ chối đề xuất." });
    }

    private static MentorReviewQuotaResponse ToResponse(StudentReviewQuota quota) =>
        new(
            quota.PlanName,
            quota.Limit,
            quota.Used,
            quota.Remaining,
            quota.PeriodStart,
            quota.PeriodEnd);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeJobReadinessLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return AllowedJobReadinessLevels.SingleOrDefault(allowed =>
            allowed.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<PortfolioResponse> ToPortfolioResponse(Portfolio portfolio, CancellationToken cancellationToken)
    {
        var projects = await dbContext.PortfolioProjects
            .AsNoTracking()
            .Where(project => project.PortfolioId == portfolio.Id)
            .OrderBy(project => project.OrderIndex)
            .Select(project => new PortfolioProjectResponse(
                project.Id,
                project.GithubRepositoryId,
                project.Title,
                project.Description,
                project.TechStackJson,
                project.ImageUrl,
                project.DemoUrl,
                project.SourceUrl,
                project.OrderIndex))
            .ToListAsync(cancellationToken);

        return new PortfolioResponse(
            portfolio.Id,
            portfolio.Slug,
            portfolio.Title,
            portfolio.Bio,
            portfolio.Theme,
            portfolio.IsPublished,
            portfolio.PublishedAt,
            portfolio.CreatedAt,
            portfolio.UpdatedAt,
            projects);
    }

    private static MentorFeedbackResponse ToMentorFeedbackResponse(
        MentorFeedback feedback,
        string mentorFullName,
        string studentFullName) =>
        new(
            feedback.Id,
            feedback.MentorId,
            mentorFullName,
            feedback.StudentId,
            studentFullName,
            feedback.PortfolioId,
            feedback.GithubRepositoryId,
            feedback.Comment,
            feedback.Rating,
            feedback.PortfolioQualityFeedback,
            feedback.TechnicalSkillsAssessment,
            feedback.ProjectQualityFeedback,
            feedback.Recommendations,
            feedback.JobReadinessLevel,
            feedback.CreatedAt,
            feedback.UpdatedAt);

    private async Task<string?> ValidateSkillPrerequisiteRequest(
        SaveSkillPrerequisiteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SkillId == Guid.Empty)
        {
            return "Kỹ năng là bắt buộc.";
        }

        if (request.PrerequisiteSkillId == Guid.Empty)
        {
            return "Kỹ năng tiên quyết là bắt buộc.";
        }

        if (request.SkillId == request.PrerequisiteSkillId)
        {
            return "Một kỹ năng không thể là tiên quyết của chính nó.";
        }

        var skillExists = await dbContext.Skills.AnyAsync(
            skill => skill.Id == request.SkillId && skill.IsActive,
            cancellationToken);
        if (!skillExists)
        {
            return "Không tìm thấy kỹ năng đang hoạt động.";
        }

        var prerequisiteSkillExists = await dbContext.Skills.AnyAsync(
            skill => skill.Id == request.PrerequisiteSkillId && skill.IsActive,
            cancellationToken);
        if (!prerequisiteSkillExists)
        {
            return "Không tìm thấy kỹ năng tiên quyết đang hoạt động.";
        }

        var duplicate = await dbContext.SkillPrerequisites.AnyAsync(
            item => item.SkillId == request.SkillId && item.PrerequisiteSkillId == request.PrerequisiteSkillId,
            cancellationToken);
        if (duplicate)
        {
            return "Quan hệ tiên quyết này đã tồn tại.";
        }

        var edges = await dbContext.SkillPrerequisites
            .AsNoTracking()
            .Select(item => new { item.SkillId, item.PrerequisiteSkillId })
            .ToListAsync(cancellationToken);

        var adjacency = edges
            .GroupBy(edge => edge.SkillId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.PrerequisiteSkillId).ToList());

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(request.PrerequisiteSkillId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == request.SkillId)
            {
                return "Không thể tạo quan hệ này vì sẽ tạo thành vòng lặp tiên quyết.";
            }

            if (!visited.Add(current))
            {
                continue;
            }

            if (adjacency.TryGetValue(current, out var next))
            {
                foreach (var skillId in next)
                {
                    queue.Enqueue(skillId);
                }
            }
        }

        return null;
    }

    private static SkillPrerequisiteResponse ToResponse(SkillPrerequisite prerequisite) =>
        new(
            prerequisite.Id,
            prerequisite.SkillId,
            prerequisite.Skill.Name,
            prerequisite.PrerequisiteSkillId,
            prerequisite.PrerequisiteSkill.Name,
            prerequisite.CreatedAt);

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
        bool hasExistingFile,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return "Tiêu đề tài nguyên học tập là bắt buộc.";
        }

        if (request.File is null || request.File.Length == 0)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                if (!hasExistingFile)
                {
                    return "URL or uploaded file is required.";
                }
            }
            else
            {
                var trimmedUrl = request.Url.Trim();
                var isLocalDownloadUrl = IsInternalLearningResourceUrl(trimmedUrl);

                if (isLocalDownloadUrl)
                {
                    if (!hasExistingFile)
                    {
                        return "URL must be the original external URL, not an internal file download URL.";
                    }
                }
                else if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out _))
                {
                    return "URL must be absolute.";
                }
            }
        }
        else
        {
            if (request.File.Length > storageOpts.MaxUploadBytes)
            {
                return $"File is too large. Maximum size is {storageOpts.MaxUploadBytes} bytes.";
            }

            if (!LearningResourceContentTypes.Contains(request.File.ContentType))
            {
                return $"Unsupported file type: {request.File.ContentType}.";
            }

            var normalizedUrl = NormalizeExternalResourceUrl(request.Url);
            if (!string.IsNullOrWhiteSpace(normalizedUrl) && !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out _))
            {
                return "URL must be absolute.";
            }
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

        var allowedLevels = new[] { "Beginner", "Intermediate", "Advanced", "Expert" };
        if (!allowedLevels.Contains(request.RequiredLevel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Cấp độ kỹ năng phải là một trong các giá trị: Beginner, Intermediate, Advanced, Expert.";
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
            ToExternalResourceUrl(resource.Url),
            resource.StorageObjectName is null ? "Link" : "File",
            resource.ContentType,
            resource.FileSize,
            GetLearningResourceFileName(resource.StorageObjectName),
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

    private static string ToExternalResourceUrl(string? url) =>
        NormalizeExternalResourceUrl(url) ?? string.Empty;

    private static string? GetLearningResourceFileName(string? storageObjectName)
    {
        if (string.IsNullOrWhiteSpace(storageObjectName))
        {
            return null;
        }

        var fileName = Path.GetFileName(storageObjectName.Replace("\\", "/"));
        var parts = fileName.Split('-', 3, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3 && parts[0].Length == 17 && parts[1].Length == 32
            ? parts[2]
            : fileName;
    }

    private static string? NormalizeExternalResourceUrl(string? url)
    {
        var trimmedUrl = url?.Trim();
        return string.IsNullOrWhiteSpace(trimmedUrl) || IsInternalLearningResourceUrl(trimmedUrl)
            ? null
            : trimmedUrl;
    }

    private static bool IsInternalLearningResourceUrl(string url) =>
        url.StartsWith("/api/storage/learning-resources/", StringComparison.OrdinalIgnoreCase);

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

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token người dùng không hợp lệ.");
    }
}

public sealed record MentorStudentSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    string? PortfolioSlug,
    string? PortfolioTitle,
    DateTimeOffset? PortfolioPublishedAt,
    string? CvUrl = null,
    string? CvName = null);

public sealed record MentorGithubRepoResponse(
    Guid Id,
    string RepoName,
    string RepoUrl,
    string? Description,
    string? AiSummary,
    string? TechStackJson,
    decimal? QualityScore,
    DateTimeOffset UpdatedAt);

public sealed record CreateMentorFeedbackRequest(
    Guid StudentId,
    Guid? PortfolioId,
    Guid? GithubRepositoryId,
    string Comment,
    int? Rating,
    string? PortfolioQualityFeedback,
    string? TechnicalSkillsAssessment,
    string? ProjectQualityFeedback,
    string? Recommendations,
    string? JobReadinessLevel);

public sealed record MentorFeedbackResponse(
    Guid Id,
    Guid MentorId,
    string MentorFullName,
    Guid StudentId,
    string StudentFullName,
    Guid? PortfolioId,
    Guid? GithubRepositoryId,
    string Comment,
    int? Rating,
    string? PortfolioQualityFeedback,
    string? TechnicalSkillsAssessment,
    string? ProjectQualityFeedback,
    string? Recommendations,
    string? JobReadinessLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record MentorReviewQuotaResponse(
    string PlanName,
    int Limit,
    int Used,
    int Remaining,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd);

public sealed record MentorViewableUserSkillResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    string? EvidenceUrl,
    string? EvidenceType,
    bool IsVerified,
    DateTimeOffset? VerifiedAt,
    Guid? VerifiedByUserId,
    string? VerifiedByName,
    string? VerifiedByRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

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

public sealed class SaveLearningResourceRequest
{
    public Guid? SkillId { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? ResourceType { get; set; }
    public string? Difficulty { get; set; }
    public int? EstimatedHours { get; set; }
    public bool? IsActive { get; set; }
    public int? LessonNumber { get; set; }
    public IFormFile? File { get; set; }
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
    string? FileName,
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

public sealed record SaveSkillPrerequisiteRequest(
    Guid SkillId,
    Guid PrerequisiteSkillId);

public sealed record SkillPrerequisiteResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    Guid PrerequisiteSkillId,
    string PrerequisiteSkillName,
    DateTimeOffset CreatedAt);

public sealed record CreateCareerRoleRequest(
    string Name,
    string? Description,
    string? Level);

public sealed record UpdateCareerRoleRequest(
    string Name,
    string? Description,
    string? Level,
    bool IsActive);
