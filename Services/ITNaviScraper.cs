using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class ITNaviScraper : IJobScraper
{
    public string SourceName => "ITNavi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarketPulseOptions _options;
    private readonly ILogger<ITNaviScraper> _logger;
    private readonly ISkillExtractor _skillExtractor;

    // Pattern to catch data-id="..."
    private static readonly Regex DataIdRegex = new Regex(
        @"data-id=""([0-9]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ITNaviScraper(
        IHttpClientFactory httpClientFactory,
        IOptions<MarketPulseOptions> options,
        ILogger<ITNaviScraper> logger,
        ISkillExtractor skillExtractor)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _skillExtractor = skillExtractor;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ITNavi scrape run via AJAX JSON endpoints.");

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        int maxPages = Math.Max(1, _options.ITNavi.MaxSitemapPages); // Reusing this cap as max pages to scan
        int yieldCount = 0;
        int maxJobs = _options.ITNavi.MaxJobsPerRun;

        for (int page = 1; page <= maxPages; page++)
        {
            if (cancellationToken.IsCancellationRequested || yieldCount >= maxJobs)
                break;

            string listUrl = $"https://itnavi.com.vn/job/search?page={page}";
            _logger.LogInformation("Scanning ITNavi page: {Url}", listUrl);

            string html;
            try
            {
                html = await httpClient.GetStringAsync(listUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load ITNavi listing page {Url}", listUrl);
                continue;
            }

            var matches = DataIdRegex.Matches(html);
            if (matches.Count == 0)
            {
                _logger.LogInformation("No more jobs found on ITNavi at page {Page}. Stopping.", page);
                break;
            }

            // Extract unique job ids
            var jobIds = matches.Select(m => m.Groups[1].Value).Distinct().ToList();

            foreach (var jobId in jobIds)
            {
                if (cancellationToken.IsCancellationRequested || yieldCount >= maxJobs)
                    break;

                if (existingExternalIds.Contains(jobId))
                {
                    continue; // Already processed in past runs
                }

                // Politeness delay
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ITNavi.DelaySeconds)), cancellationToken);

                string detailUrl = $"https://itnavi.com.vn/ajax/get-job-by-id/{jobId}";
                string json;
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, detailUrl);
                    req.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    var res = await httpClient.SendAsync(req, cancellationToken);
                    res.EnsureSuccessStatusCode();
                    json = await res.Content.ReadAsStringAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch details for ITNavi job {JobId}", jobId);
                    continue;
                }

                var scrapedJob = ParseJobFromJson(json);
                if (scrapedJob is not null)
                {
                    yield return scrapedJob;
                    yieldCount++;
                }
            }
        }

        _logger.LogInformation("ITNavi scrape completed. Yielded {YieldCount} new jobs.", yieldCount);
    }

    private ScrapedJob? ParseJobFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("success", out var successElement) || !successElement.GetBoolean())
                return null;

            if (!root.TryGetProperty("data", out var data))
                return null;

            var externalId = data.TryGetProperty("job_id", out var idProp) ? idProp.GetInt32().ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            var title = data.TryGetProperty("job_name", out var titleProp) ? titleProp.GetString() : string.Empty;
            if (string.IsNullOrWhiteSpace(title)) return null;

            var sourceUrl = data.TryGetProperty("job_slug", out var urlProp) ? urlProp.GetString() : string.Empty;
            var companyName = data.TryGetProperty("company_name", out var compProp) ? compProp.GetString() : null;
            var location = data.TryGetProperty("job_place", out var locProp) ? locProp.GetString() : null;
            
            // Description requires combining multiple html fields for best context
            var descHtml = data.TryGetProperty("job_description", out var descProp) ? descProp.GetString() ?? "" : "";
            var reqHtml = data.TryGetProperty("job_requirement", out var reqpProp) ? reqpProp.GetString() ?? "" : "";
            var fullDesc = (descHtml + "\n" + reqHtml).Trim();
            
            // Salary handling
            decimal? minMil = null;
            decimal? maxMil = null;
            string? salaryText = data.TryGetProperty("job_salary", out var salProp) ? salProp.GetString() : null;
            
            // Date handling
            DateTimeOffset? postedAt = null;
            if (data.TryGetProperty("job_published_at", out var pubProp))
            {
                var pubStr = pubProp.GetString();
                if (DateTime.TryParse(pubStr, out var parsedDate))
                {
                    postedAt = new DateTimeOffset(parsedDate, TimeSpan.Zero);
                }
            }

            // IT Filter check: Only yield if it actually looks like an IT job
            // Even though ITNavi is 99% IT, double check using our SkillExtractor.
            if (!_skillExtractor.LooksLikeItJob(title, fullDesc))
            {
                 // Exclude non-IT admin/sales jobs accidentally posted
                 return null;
            }

            return new ScrapedJob
            {
                ExternalId = externalId,
                Title = title,
                CompanyName = companyName,
                Location = location,
                SalaryText = salaryText,
                SalaryMinMillionVnd = minMil,
                SalaryMaxMillionVnd = maxMil,
                Description = fullDesc,
                SourceUrl = sourceUrl ?? $"https://itnavi.com.vn/job-detail/{externalId}",
                PostedAt = postedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse ITNavi JSON response");
            return null;
        }
    }
}
