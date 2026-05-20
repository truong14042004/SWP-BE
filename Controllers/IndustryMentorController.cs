using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/industry-mentor")]
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
            .Select(user => new
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

    [HttpGet("students/{studentId:guid}/portfolio")]
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

    [HttpGet("students/{studentId:guid}/github")]
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
