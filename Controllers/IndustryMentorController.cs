using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/industry-mentor")] //lay danh sach hoc sinh da publish portfolio
[Authorize(Roles = UserRoles.IndustryMentor)]
public sealed class IndustryMentorController(AppDbContext dbContext) : ControllerBase
{
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
                    .FirstOrDefault()
            })
            .Where(item => item.Portfolio is not null)
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
                item.Portfolio.PublishedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    [HttpGet("students/{studentId:guid}/portfolio")] //xem chi tiet portfolio cua hoc sinh
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

    [HttpGet("students/{studentId:guid}/github")] //lay danh sach repo cua hoc sinh
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

    [HttpPost("feedback")] //mentor tao feedback cho hoc sinh
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

        var mentorId = GetCurrentUserId(); //lay mentor id
        var student = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.StudentId && item.Role == UserRoles.Student && item.IsActive,
                cancellationToken);
        if (student is null)
        {
            return NotFound(new { message = "Student was not found." });
        }

        if (request.PortfolioId is not null) //neu co portfolio
        {
            var hasPortfolio = await dbContext.Portfolios.AnyAsync(
                item => item.Id == request.PortfolioId && item.UserId == request.StudentId,
                cancellationToken); //kiem tra portfolio co thuoc ve hoc sinh khong
            if (!hasPortfolio)
            {
                return BadRequest(new { message = "Portfolio does not belong to the student." }); //tra ve loi neu portfolio khong thuoc ve hoc sinh
            }
        }

        if (request.GithubRepositoryId is not null) //kiem tra repo co thuoc ve hoc sinh khong
        {
            var hasRepository = await dbContext.GithubRepositories.AnyAsync(
                item => item.Id == request.GithubRepositoryId && item.UserId == request.StudentId,
                cancellationToken);
            if (!hasRepository)
            {
                return BadRequest(new { message = "GitHub repository does not belong to the student." });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var feedback = new MentorFeedback //tao feedback 
        {
            Id = Guid.NewGuid(),
            MentorId = mentorId,
            StudentId = request.StudentId,
            PortfolioId = request.PortfolioId,
            GithubRepositoryId = request.GithubRepositoryId,
            Comment = request.Comment.Trim(),
            Rating = request.Rating,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.MentorFeedbacks.Add(feedback);
        await dbContext.SaveChangesAsync();

        var mentor = await dbContext.Users //lay mentor name de luu vo db
            .AsNoTracking()
            .SingleAsync(item => item.Id == mentorId, cancellationToken);

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
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
    }

    [HttpGet("students/{studentId:guid}/feedback")] //lay danh sach feedback cua mentor theo tung hoc sinh
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
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(responses);
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
    DateTimeOffset? PortfolioPublishedAt);

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
    int? Rating);

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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
