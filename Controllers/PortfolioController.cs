using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed partial class PortfolioController(AppDbContext dbContext) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PortfolioResponse>> GetMine(CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
            .Where(item => item.UserId == GetCurrentUserId())
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Portfolio was not found." });
        }

        return Ok(await ToResponse(portfolio, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PortfolioResponse>> Create(
        SavePortfolioRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? await CreateUniqueSlug(request.Title, cancellationToken)
            : await CreateUniqueSlug(request.Slug, cancellationToken);

        var portfolio = new Portfolio
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Slug = slug,
            Title = request.Title.Trim(),
            Bio = request.Bio?.Trim(),
            Theme = string.IsNullOrWhiteSpace(request.Theme) ? "Default" : request.Theme.Trim(),
            IsPublished = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Portfolios.Add(portfolio);
        try
        {
            await SyncProjects(portfolio.Id, userId, request.Projects, now, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPublic), new { slug = portfolio.Slug }, await ToResponse(portfolio, cancellationToken));
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<PortfolioResponse>> Update(
        SavePortfolioRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var portfolio = await dbContext.Portfolios
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Portfolio was not found." });
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            portfolio.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Slug)
            && !portfolio.Slug.Equals(request.Slug.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            portfolio.Slug = await CreateUniqueSlug(request.Slug, cancellationToken, portfolio.Id);
        }

        portfolio.Bio = request.Bio?.Trim();
        portfolio.Theme = string.IsNullOrWhiteSpace(request.Theme) ? portfolio.Theme : request.Theme.Trim();
        portfolio.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.Projects is not null)
        {
            try
            {
                await SyncProjects(portfolio.Id, userId, request.Projects, portfolio.UpdatedAt, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponse(portfolio, cancellationToken));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<PortfolioResponse>> GetPublic(string slug, CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Slug == slug.Trim().ToLowerInvariant() && item.IsPublished,
                cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Published portfolio was not found." });
        }

        return Ok(await ToResponse(portfolio, cancellationToken));
    }

    [Authorize]
    [HttpPost("publish")]
    public async Task<ActionResult<PortfolioResponse>> Publish(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var portfolio = await dbContext.Portfolios
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Portfolio was not found." });
        }

        var now = DateTimeOffset.UtcNow;
        portfolio.IsPublished = true;
        portfolio.PublishedAt ??= now;
        portfolio.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponse(portfolio, cancellationToken));
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpPost("unpublish")]
    public async Task<ActionResult<PortfolioResponse>> Unpublish(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var portfolio = await dbContext.Portfolios
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (portfolio is null)
        {
            return NotFound(new { message = "Portfolio was not found." });
        }

        portfolio.IsPublished = false;
        portfolio.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(await ToResponse(portfolio, cancellationToken));
    }

    private async Task<PortfolioResponse> ToResponse(Portfolio portfolio, CancellationToken cancellationToken)
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

    private async Task SyncProjects(
        Guid portfolioId,
        Guid userId,
        IReadOnlyList<SavePortfolioProjectRequest>? projectRequests,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (projectRequests is null)
        {
            return;
        }

        var existingProjects = await dbContext.PortfolioProjects
            .Where(project => project.PortfolioId == portfolioId)
            .ToListAsync(cancellationToken);
        dbContext.PortfolioProjects.RemoveRange(existingProjects);

        if (projectRequests.Count == 0)
        {
            return;
        }

        var repositoryIds = projectRequests
            .Where(project => project.GithubRepositoryId is not null)
            .Select(project => project.GithubRepositoryId!.Value)
            .Distinct()
            .ToArray();
        var repositories = await dbContext.GithubRepositories
            .Where(repository => repository.UserId == userId && repositoryIds.Contains(repository.Id))
            .ToDictionaryAsync(repository => repository.Id, cancellationToken);

        var projects = new List<PortfolioProject>();
        for (var index = 0; index < projectRequests.Count; index++)
        {
            var request = projectRequests[index];
            GithubRepository? repository = null;
            if (request.GithubRepositoryId is not null
                && !repositories.TryGetValue(request.GithubRepositoryId.Value, out repository))
            {
                throw new InvalidOperationException("One or more GitHub repositories do not belong to the current user.");
            }

            var title = !string.IsNullOrWhiteSpace(request.Title)
                ? request.Title.Trim()
                : repository?.RepoName;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("Portfolio project title is required.");
            }

            projects.Add(new PortfolioProject
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                GithubRepositoryId = repository?.Id,
                Title = title,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? repository?.AiSummary ?? repository?.Description
                    : request.Description.Trim(),
                TechStackJson = string.IsNullOrWhiteSpace(request.TechStackJson)
                    ? repository?.TechStackJson
                    : request.TechStackJson.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                DemoUrl = request.DemoUrl?.Trim(),
                SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl)
                    ? repository?.RepoUrl
                    : request.SourceUrl.Trim(),
                OrderIndex = request.OrderIndex ?? index,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        dbContext.PortfolioProjects.AddRange(projects);
    }

    private async Task<string> CreateUniqueSlug(
        string value,
        CancellationToken cancellationToken,
        Guid? currentPortfolioId = null)
    {
        var baseSlug = SlugInvalidCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "portfolio";
        }

        var slug = baseSlug;
        var suffix = 1;
        while (await dbContext.Portfolios.AnyAsync(
            portfolio => portfolio.Slug == slug && portfolio.Id != currentPortfolioId,
            cancellationToken))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugInvalidCharacters();
}

public sealed record SavePortfolioRequest(
    string? Slug,
    string? Title,
    string? Bio,
    string? Theme,
    IReadOnlyList<SavePortfolioProjectRequest>? Projects);

public sealed record SavePortfolioProjectRequest(
    Guid? GithubRepositoryId,
    string? Title,
    string? Description,
    string? TechStackJson,
    string? ImageUrl,
    string? DemoUrl,
    string? SourceUrl,
    int? OrderIndex);

public sealed record PortfolioResponse(
    Guid Id,
    string Slug,
    string Title,
    string? Bio,
    string? Theme,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PortfolioProjectResponse> Projects);

public sealed record PortfolioProjectResponse(
    Guid Id,
    Guid? GithubRepositoryId,
    string Title,
    string? Description,
    string? TechStackJson,
    string? ImageUrl,
    string? DemoUrl,
    string? SourceUrl,
    int OrderIndex);
