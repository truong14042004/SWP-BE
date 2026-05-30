using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Data;
using SWP_BE.Options;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/market")]
public sealed class MarketPulseController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMarketPulseRunner _runner;
    private readonly MarketPulseOptions _options;

    public MarketPulseController(
        AppDbContext dbContext,
        IMarketPulseRunner runner,
        IOptions<MarketPulseOptions> options)
    {
        _dbContext = dbContext;
        _runner = runner;
        _options = options.Value;
    }

    [HttpGet("keywords/trending")]
    public async Task<IActionResult> GetTrendingKeywords(
        [FromQuery] int days = 30,
        [FromQuery] int top = 20,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 365);
        top = Math.Clamp(top, 1, 200);
        var since = DateTime.UtcNow.AddDays(-days);

        var trending = await _dbContext.JobSkillMentions
            .Where(mention => (mention.JobPost.PostedAt ?? mention.JobPost.ScrapedAt) >= since)
            .GroupBy(mention => mention.Keyword)
            .Select(group => new
            {
                keyword = group.Key,
                jobCount = group.Select(item => item.JobPostId).Distinct().Count(),
                totalMentions = group.Sum(item => item.MentionCount),
                skillId = group.Select(item => item.SkillId).FirstOrDefault(),
            })
            .OrderByDescending(item => item.jobCount)
            .ThenByDescending(item => item.totalMentions)
            .Take(top)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            windowDays = days,
            generatedAt = DateTimeOffset.UtcNow,
            items = trending,
        });
    }

    [HttpGet("keywords/{keyword}/daily")]
    public async Task<IActionResult> GetKeywordDaily(
        string keyword,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { message = "Keyword là bắt buộc." });
        }
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);
        var loweredKeyword = keyword.ToLower();

        var raw = await _dbContext.JobSkillMentions
            .Where(mention => mention.Keyword.ToLower() == loweredKeyword
                && (mention.JobPost.PostedAt ?? mention.JobPost.ScrapedAt) >= since)
            .Select(mention => new
            {
                postedAt = mention.JobPost.PostedAt,
                scrapedAt = mention.JobPost.ScrapedAt,
                jobId = mention.JobPostId,
                mentions = mention.MentionCount,
            })
            .ToListAsync(cancellationToken);

        var grouped = raw
            .GroupBy(item => DateOnly.FromDateTime((item.postedAt ?? item.scrapedAt).UtcDateTime))
            .Select(group => new
            {
                date = group.Key,
                jobCount = group.Select(item => item.jobId).Distinct().Count(),
                totalMentions = group.Sum(item => item.mentions),
            })
            .OrderBy(item => item.date)
            .ToList();

        var lastScrapedAt = await _dbContext.JobPosts
            .OrderByDescending(post => post.ScrapedAt)
            .Select(post => (DateTimeOffset?)post.ScrapedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            keyword,
            days,
            lastScrapedAt,
            points = grouped,
        });
    }

    [HttpGet("keywords/{keyword}/trend")]
    public async Task<IActionResult> GetKeywordTrend(
        string keyword,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { message = "Keyword là bắt buộc." });
        }
        days = Math.Clamp(days, 1, 365);
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));

        var trend = await _dbContext.KeywordTrendSnapshots
            .Where(snapshot => snapshot.Keyword.ToLower() == keyword.ToLower()
                && snapshot.SnapshotDate >= since)
            .OrderBy(snapshot => snapshot.SnapshotDate)
            .Select(snapshot => new
            {
                date = snapshot.SnapshotDate,
                jobCount = snapshot.JobCount,
                totalMentions = snapshot.TotalMentions,
                windowDays = snapshot.WindowDays,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            keyword,
            days,
            points = trend,
        });
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? keyword,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.JobPosts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(post => post.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var loweredKeyword = keyword.ToLower();
            query = query.Where(post => _dbContext.JobSkillMentions
                .Any(mention => mention.JobPostId == post.Id
                    && mention.Keyword.ToLower() == loweredKeyword));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(post => post.ScrapedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(post => new
            {
                post.Id,
                post.Source,
                post.Title,
                post.CompanyName,
                post.Location,
                post.SalaryText,
                post.SalaryMinMillionVnd,
                post.SalaryMaxMillionVnd,
                post.SourceUrl,
                post.PostedAt,
                post.ScrapedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items,
        });
    }

    [HttpGet("jobs/{id}")]
    public async Task<IActionResult> GetJobDetail(Guid id, CancellationToken cancellationToken)
    {
        var post = await _dbContext.JobPosts
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Source,
                item.Title,
                item.CompanyName,
                item.Location,
                item.SalaryText,
                item.Description,
                item.SourceUrl,
                item.PostedAt,
                item.ScrapedAt,
                keywords = _dbContext.JobSkillMentions
                    .Where(mention => mention.JobPostId == item.Id)
                    .OrderByDescending(mention => mention.MentionCount)
                    .Select(mention => new { mention.Keyword, mention.MentionCount, mention.SkillId })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null)
        {
            return NotFound(new { message = "Không tìm thấy job." });
        }
        return Ok(post);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var totalJobs = await _dbContext.JobPosts.CountAsync(cancellationToken);
        var totalKeywords = await _dbContext.JobSkillMentions
            .Select(mention => mention.Keyword)
            .Distinct()
            .CountAsync(cancellationToken);
        var lastScrapedAt = await _dbContext.JobPosts
            .OrderByDescending(post => post.ScrapedAt)
            .Select(post => (DateTimeOffset?)post.ScrapedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var sources = await _dbContext.JobPosts
            .GroupBy(post => post.Source)
            .Select(group => new { source = group.Key, count = group.Count() })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            totalJobs,
            totalKeywords,
            lastScrapedAt,
            sources,
        });
    }
    [HttpPost("scrape-now")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ScrapeNow(CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("re-extract")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReExtract(CancellationToken cancellationToken)
    {
        var result = await _runner.ReExtractAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("internal/scrape")]
    [AllowAnonymous]
    public async Task<IActionResult> InternalScrape(
        [FromServices] IOptions<InternalAuthOptions> internalAuthOptions,
        CancellationToken cancellationToken)
    {
        var authError = ValidateInternalToken(internalAuthOptions);
        if (authError is not null)
        {
            return authError;
        }

        var result = await _runner.RunAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("internal/purge-non-it")]
    [AllowAnonymous]
    public async Task<IActionResult> InternalPurgeNonIt(
        [FromServices] IOptions<InternalAuthOptions> internalAuthOptions,
        [FromQuery] string source,
        CancellationToken cancellationToken)
    {
        var authError = ValidateInternalToken(internalAuthOptions);
        if (authError is not null)
        {
            return authError;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return BadRequest(new { message = "Query parameter 'source' is required." });
        }

        var removed = await _runner.PurgeNonItJobsAsync(source, cancellationToken);
        return Ok(new { source, removed });
    }

    // Constant-time comparison of the X-Internal-Token header against the
    // configured token. Returns an error result when invalid, or null when the
    // caller is authorized.
    private IActionResult? ValidateInternalToken(IOptions<InternalAuthOptions> internalAuthOptions)
    {
        var expected = internalAuthOptions.Value.Token;
        if (string.IsNullOrWhiteSpace(expected))
        {
            return StatusCode(503, new { message = "Internal auth not configured." });
        }

        if (!Request.Headers.TryGetValue("X-Internal-Token", out var provided))
        {
            return Unauthorized();
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        if (providedBytes.Length != expectedBytes.Length
            || !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
        {
            return Unauthorized();
        }

        return null;
    }

    [HttpGet("config")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            enabled = _options.Enabled,
            scheduleHour = _options.ScheduleHour,
            scheduleMinute = _options.ScheduleMinute,
            timeZoneId = _options.TimeZoneId,
            trendWindowDays = _options.TrendWindowDays,
            sources = new
            {
                topDev = new
                {
                    enabled = _options.TopDev.Enabled,
                    baseUrl = _options.TopDev.BaseUrl,
                    sitemapIndexPath = _options.TopDev.SitemapIndexPath,
                    maxSitemapPages = _options.TopDev.MaxSitemapPages,
                    maxJobsPerRun = _options.TopDev.MaxJobsPerRun,
                },
            },
        });
    }
}
