using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP_BE.Models;
using SWP_BE.Options;
using System.Text.RegularExpressions;
using System.Xml;

namespace SWP_BE.Services;

public sealed class VietnamWorksScraper : IJobScraper
{
    private readonly ILogger<VietnamWorksScraper> _logger;
    private readonly MarketPulseOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkillExtractor _skillExtractor;

    public string SourceName => "VietnamWorks";

    public VietnamWorksScraper(
        ILogger<VietnamWorksScraper> logger,
        IOptions<MarketPulseOptions> options,
        IHttpClientFactory httpClientFactory,
        ISkillExtractor skillExtractor)
    {
        _logger = logger;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _skillExtractor = skillExtractor;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(IReadOnlySet<string> existingExternalIds, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = _options.VietnamWorks;
        if (config == null || !config.Enabled)
        {
            _logger.LogInformation("VietnamWorks scraper is disabled.");
            yield break;
        }

        using var httpClient = _httpClientFactory.CreateClient("VietnamWorksScraper");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        _logger.LogInformation("Fetching VW Sitemap from {Url}", config.SitemapUrl);
        var sitemapXml = "";
        try
        {
            sitemapXml = await httpClient.GetStringAsync(config.SitemapUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch VW sitemap");
            yield break;
        }

        var jobUrls = new List<string>();
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(sitemapXml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("sm", "http://www.sitemaps.org/schemas/sitemap/0.9");
            
            var locNodes = doc.SelectNodes("//sm:url/sm:loc", nsmgr);
            if (locNodes != null)
            {
                foreach (XmlNode node in locNodes)
                {
                    var loc = node.InnerText;
                    if (loc.Contains("-jv"))
                    {
                        jobUrls.Add(loc);
                        if (jobUrls.Count >= config.MaxSitemapJobs) break; // Chỉ quét n job đầu tiên
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse VW sitemap XML");
            yield break;
        }

        _logger.LogInformation("Found {Count} job URLs from VW sitemap. Start extracting.", jobUrls.Count);

        int yieldCount = 0;
        int maxJobs = config.MaxJobsPerRun;

        foreach (var jobUrl in jobUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (yieldCount >= maxJobs)
            {
                _logger.LogInformation("VW reached MaxJobsPerRun limit ({Limit}).", maxJobs);
                break;
            }

            // Extract Id from Url: .../tieu-de-job-ID-jv
            var idMatch = Regex.Match(jobUrl, @"-(\d+)-jv\/?(\?.*)?$");
            if (!idMatch.Success) continue;
            var externalId = idMatch.Groups[1].Value;

            if (existingExternalIds.Contains(externalId))
            {
                // Bỏ qua vì đã cào
                continue;
            }

            // Politeness delay
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, config.DelaySeconds)), cancellationToken);

            var job = await TryScrapeDetailAsync(httpClient, jobUrl, externalId, cancellationToken);
            if (job != null)
            {
                if (_skillExtractor.LooksLikeItJob(job.Title, job.Description))
                {
                    yield return job;
                    yieldCount++;
                    _logger.LogInformation("VW Scraped IT job: {Title} ({Url})", job.Title, jobUrl);
                }
                else
                {
                    _logger.LogDebug("VW Job rejected (Not IT): {Title}", job.Title);
                }
            }
        }
    }

    private async Task<ScrapedJob?> TryScrapeDetailAsync(HttpClient client, string jobUrl, string externalId, CancellationToken cancellationToken)
    {
        try
        {
            var html = await client.GetStringAsync(jobUrl, cancellationToken);
            
            // Dùng HtmlAgilityPack để lọc bỏ Menu/Footer rác chứa từ khóa IT
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            
            // Xóa rác
            var garbageSelectors = new[] { "//nav", "//footer", "//header", "//aside", "//*[contains(@class, 'footer')]", "//*[contains(@class, 'header')]", "//*[@role='navigation']" };
            foreach (var selector in garbageSelectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(selector);
                if (nodes != null)
                {
                    foreach (var node in nodes) node.Remove();
                }
            }

            var titleMatch = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
            if (!titleMatch.Success) return null;

            var fullTitle = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value);
            var title = fullTitle;
            var company = "VietnamWorks";

            var titleRegex = Regex.Match(fullTitle, @"Tuyển\s+(.*?)\s+tại\s+(.*?)(?:\s+T\d+/\d+|\s*\||$)", RegexOptions.IgnoreCase);
            if (titleRegex.Success)
            {
                title = titleRegex.Groups[1].Value.Trim();
                company = titleRegex.Groups[2].Value.Trim();
            }

            var plainTextDesc = doc.DocumentNode.InnerText ?? "";
            plainTextDesc = System.Net.WebUtility.HtmlDecode(plainTextDesc);
            
            // Xóa bớt khoảng trắng thừa
            plainTextDesc = Regex.Replace(plainTextDesc, @"\s+", " ").Trim();

            return new ScrapedJob
            {
                ExternalId = externalId,
                Title = title,
                CompanyName = company,
                Description = plainTextDesc.Length > 8000 ? plainTextDesc.Substring(0, 8000) : plainTextDesc,
                SourceUrl = jobUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("VW Detail scrape failed for {Url}: {Msg}", jobUrl, ex.Message);
            return null;
        }
    }
}
