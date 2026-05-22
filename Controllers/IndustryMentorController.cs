using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/industry-mentor")] //Industry Mentor review portfolio sinh vien da publish
[Authorize(Roles = UserRoles.IndustryMentor)]
public sealed class IndustryMentorController(
    AppDbContext dbContext,
    IStudentReviewQuotaService quotaService) : ControllerBase
{

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
            return NotFound(new { message = "Portfolio was not found." });
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
            return NotFound(new { message = "User skill was not found." });
        }

        if (userSkill.IsVerified)
        {
            return Conflict(new { message = "Skill is already verified." });
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
            return NotFound(new { message = "User skill was not found." });
        }

        if (!userSkill.IsVerified)
        {
            return Conflict(new { message = "Skill is not currently verified." });
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
            return BadRequest(new { message = "Comment is required." });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        if (!string.IsNullOrWhiteSpace(request.JobReadinessLevel)
            && !AllowedJobReadinessLevels.Contains(request.JobReadinessLevel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = $"JobReadinessLevel must be one of: {string.Join(", ", AllowedJobReadinessLevels)}."
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
            return NotFound(new { message = "Student was not found." });
        }

        if (request.PortfolioId is not null) //kiem tra portfolio thuoc ve sinh vien
        {
            var hasPortfolio = await dbContext.Portfolios.AnyAsync(
                item => item.Id == request.PortfolioId && item.UserId == request.StudentId,
                cancellationToken);
            if (!hasPortfolio)
            {
                return BadRequest(new { message = "Portfolio does not belong to the student." });
            }
        }

        if (request.GithubRepositoryId is not null) //kiem tra repo thuoc ve sinh vien
        {
            var hasRepository = await dbContext.GithubRepositories.AnyAsync(
                item => item.Id == request.GithubRepositoryId && item.UserId == request.StudentId,
                cancellationToken);
            if (!hasRepository)
            {
                return BadRequest(new { message = "GitHub repository does not belong to the student." });
            }
        }

        //task 3: tru mentor review usage theo subscription plan cua sinh vien
        var quota = await quotaService.GetQuotaAsync(request.StudentId, cancellationToken);
        if (quota.Remaining <= 0)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, new
            {
                message = $"Sinh vien da het luot mentor review ({quota.Used}/{quota.Limit}). Vui long nang cap goi de tiep tuc.",
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
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.StudentId,
            Type = "PortfolioFeedbackReceived",
            Title = "Bạn nhận được feedback mới",
            Message = $"{mentor.FullName} đã gửi feedback portfolio cho bạn.",
            LinkUrl = "#portfolio",
            IsRead = false,
            CreatedAt = now,
            PayloadJson = JsonSerializer.Serialize(new
            {
                feedbackId = feedback.Id,
                mentorId,
                mentorName = mentor.FullName,
                rating = feedback.Rating,
                jobReadinessLevel = feedback.JobReadinessLevel,
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);

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

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
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
