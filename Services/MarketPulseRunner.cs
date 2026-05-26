using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class MarketPulseRunner : IMarketPulseRunner
{
    private readonly AppDbContext _dbContext;
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly ISkillExtractor _skillExtractor;
    private readonly MarketPulseOptions _options;
    private readonly ILogger<MarketPulseRunner> _logger;

    public MarketPulseRunner(
        AppDbContext dbContext,
        IEnumerable<IJobScraper> scrapers,
        ISkillExtractor skillExtractor,
        IOptions<MarketPulseOptions> options,
        ILogger<MarketPulseRunner> logger)
    {
        _dbContext = dbContext;
        _scrapers = scrapers;
        _skillExtractor = skillExtractor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MarketPulseRunResult> ReExtractAllAsync(CancellationToken cancellationToken)
    {
        var result = new MarketPulseRunResult { StartedAt = DateTimeOffset.UtcNow };

        var skillByName = await BuildSkillLookupAsync(cancellationToken);

        var allMentions = await _dbContext.JobSkillMentions.ToListAsync(cancellationToken);
        if (allMentions.Count > 0)
        {
            _dbContext.JobSkillMentions.RemoveRange(allMentions);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var jobs = await _dbContext.JobPosts
            .Select(post => new { post.Id, post.Title, post.Description })
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var corpus = $"{job.Title}\n{job.Description}";
            var extracted = _skillExtractor.Extract(corpus);
            foreach (var item in extracted)
            {
                skillByName.TryGetValue(item.Keyword, out var skillId);
                _dbContext.JobSkillMentions.Add(new JobSkillMention
                {
                    Id = Guid.NewGuid(),
                    JobPostId = job.Id,
                    Keyword = item.Keyword,
                    SkillId = skillId == Guid.Empty ? null : skillId,
                    MentionCount = item.Count,
                    CreatedAt = now,
                });
                result.MentionsCreated++;
            }
            result.JobsFetched++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        result.SnapshotsCreated = await CreateSnapshotsAsync(skillByName, cancellationToken);
        result.FinishedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "MarketPulse re-extract done: jobs={Jobs}, mentions={Mentions}, snapshots={Snapshots}.",
            result.JobsFetched, result.MentionsCreated, result.SnapshotsCreated);
        return result;
    }

    private async Task<Dictionary<string, Guid>> BuildSkillLookupAsync(CancellationToken cancellationToken)
    {
        var skillLookup = await _dbContext.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive)
            .Select(skill => new { skill.Id, skill.Name })
            .ToListAsync(cancellationToken);

        return skillLookup
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<MarketPulseRunResult> RunAsync(CancellationToken cancellationToken)
    {
        var result = new MarketPulseRunResult { StartedAt = DateTimeOffset.UtcNow };

        var skillLookup = await _dbContext.Skills
            .AsNoTracking()
            .Where(skill => skill.IsActive)
            .Select(skill => new { skill.Id, skill.Name })
            .ToListAsync(cancellationToken);

        var skillByName = skillLookup
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var scraper in _scrapers)
        {
            var existingExternalIdList = await _dbContext.JobPosts
                .AsNoTracking()
                .Where(post => post.Source == scraper.SourceName)
                .Select(post => post.ExternalId)
                .ToListAsync(cancellationToken);
            var existingExternalIds = existingExternalIdList.ToHashSet(StringComparer.OrdinalIgnoreCase);

            await foreach (var scrapedJob in scraper.ScrapeAsync(existingExternalIds, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                result.JobsFetched++;

                try
                {
                    var (created, updated, mentions) = await UpsertJobAsync(
                        scraper.SourceName, scrapedJob, skillByName, cancellationToken);
                    if (created) result.JobsInserted++;
                    if (updated) result.JobsUpdated++;
                    result.MentionsCreated += mentions;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to persist scraped job {ExternalId} from {Source}.",
                        scrapedJob.ExternalId, scraper.SourceName);
                    result.Errors.Add($"{scraper.SourceName}:{scrapedJob.ExternalId}: {exception.Message}");
                }
            }
        }

        result.SnapshotsCreated = await CreateSnapshotsAsync(skillByName, cancellationToken);
        await PurgeOldSnapshotsAsync(cancellationToken);

        result.FinishedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "MarketPulse run done: fetched={Fetched}, inserted={Inserted}, updated={Updated}, mentions={Mentions}, snapshots={Snapshots}, errors={Errors}.",
            result.JobsFetched, result.JobsInserted, result.JobsUpdated, result.MentionsCreated, result.SnapshotsCreated, result.Errors.Count);
        return result;
    }

    private async Task<(bool Created, bool Updated, int Mentions)> UpsertJobAsync(
        string source,
        ScrapedJob scrapedJob,
        Dictionary<string, Guid> skillByName,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.JobPosts
            .FirstOrDefaultAsync(
                post => post.Source == source && post.ExternalId == scrapedJob.ExternalId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var created = false;
        var updated = false;

        if (existing is null)
        {
            existing = new JobPost
            {
                Id = Guid.NewGuid(),
                Source = source,
                ExternalId = scrapedJob.ExternalId,
                Title = scrapedJob.Title,
                CompanyName = scrapedJob.CompanyName,
                Location = scrapedJob.Location,
                SalaryText = scrapedJob.SalaryText,
                SalaryMinMillionVnd = scrapedJob.SalaryMinMillionVnd,
                SalaryMaxMillionVnd = scrapedJob.SalaryMaxMillionVnd,
                Description = scrapedJob.Description,
                SourceUrl = scrapedJob.SourceUrl,
                PostedAt = scrapedJob.PostedAt,
                ScrapedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _dbContext.JobPosts.Add(existing);
            created = true;
        }
        else
        {
            existing.Title = scrapedJob.Title;
            existing.CompanyName = scrapedJob.CompanyName;
            existing.Location = scrapedJob.Location;
            existing.SalaryText = scrapedJob.SalaryText;
            existing.SalaryMinMillionVnd = scrapedJob.SalaryMinMillionVnd;
            existing.SalaryMaxMillionVnd = scrapedJob.SalaryMaxMillionVnd;
            existing.Description = scrapedJob.Description;
            existing.SourceUrl = scrapedJob.SourceUrl;
            existing.PostedAt = scrapedJob.PostedAt ?? existing.PostedAt;
            existing.ScrapedAt = now;
            existing.UpdatedAt = now;
            updated = true;
        }

        var corpus = $"{existing.Title}\n{existing.Description}";
        var extracted = _skillExtractor.Extract(corpus);

        var oldMentions = await _dbContext.JobSkillMentions
            .Where(mention => mention.JobPostId == existing.Id)
            .ToListAsync(cancellationToken);
        if (oldMentions.Count > 0)
        {
            _dbContext.JobSkillMentions.RemoveRange(oldMentions);
        }

        foreach (var extractedKeyword in extracted)
        {
            skillByName.TryGetValue(extractedKeyword.Keyword, out var skillId);
            _dbContext.JobSkillMentions.Add(new JobSkillMention
            {
                Id = Guid.NewGuid(),
                JobPostId = existing.Id,
                Keyword = extractedKeyword.Keyword,
                SkillId = skillId == Guid.Empty ? null : skillId,
                MentionCount = extractedKeyword.Count,
                CreatedAt = now,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (created, updated, extracted.Count);
    }

    private async Task<int> CreateSnapshotsAsync(
        Dictionary<string, Guid> skillByName,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowStart = DateTime.UtcNow.AddDays(-_options.TrendWindowDays);

        var aggregated = await _dbContext.JobSkillMentions
            .Where(mention => mention.JobPost.ScrapedAt >= windowStart)
            .GroupBy(mention => mention.Keyword)
            .Select(group => new
            {
                Keyword = group.Key,
                JobCount = group.Select(item => item.JobPostId).Distinct().Count(),
                TotalMentions = group.Sum(item => item.MentionCount),
            })
            .ToListAsync(cancellationToken);

        var existingToday = await _dbContext.KeywordTrendSnapshots
            .Where(snapshot => snapshot.SnapshotDate == today
                && snapshot.WindowDays == _options.TrendWindowDays)
            .ToListAsync(cancellationToken);
        if (existingToday.Count > 0)
        {
            _dbContext.KeywordTrendSnapshots.RemoveRange(existingToday);
        }

        var now = DateTimeOffset.UtcNow;
        var created = 0;

        foreach (var item in aggregated)
        {
            skillByName.TryGetValue(item.Keyword, out var skillId);
            _dbContext.KeywordTrendSnapshots.Add(new KeywordTrendSnapshot
            {
                Id = Guid.NewGuid(),
                Keyword = item.Keyword,
                SkillId = skillId == Guid.Empty ? null : skillId,
                SnapshotDate = today,
                WindowDays = _options.TrendWindowDays,
                JobCount = item.JobCount,
                TotalMentions = item.TotalMentions,
                CreatedAt = now,
            });
            created++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task PurgeOldSnapshotsAsync(CancellationToken cancellationToken)
    {
        if (_options.SnapshotRetentionDays <= 0)
        {
            return;
        }
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-_options.SnapshotRetentionDays));
        await _dbContext.KeywordTrendSnapshots
            .Where(snapshot => snapshot.SnapshotDate < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
