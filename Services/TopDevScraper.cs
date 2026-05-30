using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

/// <summary>
/// Scrapes IT job postings from topdev.vn.
///
/// Unlike TopCV, TopDev exposes a public sitemap and server-renders a
/// schema.org <c>JobPosting</c> JSON-LD block inside every detail page, so we
/// can rely on a plain <see cref="HttpClient"/> plus <see cref="JsonDocument"/>
/// instead of a headless browser, rotating proxies, and Cloudflare evasion.
///
/// Flow:
///   sitemap index (/sitemap-jobs.xml) -> sub-sitemaps -> /detail-jobs/... URLs
///   -> detail page -> JSON-LD JobPosting -> <see cref="ScrapedJob"/>.
/// </summary>
public sealed class TopDevScraper : IJobScraper
{
    public string SourceName => "TopDev";

    private readonly TopDevScraperOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkillExtractor _skillExtractor;
    private readonly ILogger<TopDevScraper> _logger;

    // Trailing numeric segment of a detail URL is the stable external id,
    // e.g. ".../...-mbbank-2104766" -> "2104766".
    private static readonly Regex ExternalIdFromUrl =
        new(@"-(\d+)$", RegexOptions.Compiled);

    // <script type="application/ld+json"> ... </script> (any attribute order).
    private static readonly Regex LdJsonScript = new(
        @"<script\b[^>]*\btype\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // Approximate USD -> VND rate, only used when a posting is denominated in USD.
    private const decimal UsdToVnd = 25_000m;

    public TopDevScraper(
        IOptions<MarketPulseOptions> options,
        IHttpClientFactory httpClientFactory,
        ISkillExtractor skillExtractor,
        ILogger<TopDevScraper> logger)
    {
        _options = options.Value.TopDev;
        _httpClientFactory = httpClientFactory;
        _skillExtractor = skillExtractor;
        _logger = logger;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TopDev scraper is disabled via configuration.");
            yield break;
        }

        _logger.LogInformation("Starting TopDev scrape run via sitemap + JSON-LD.");

        using var client = CreateClient();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var yielded = 0;

        var subSitemaps = await GetSubSitemapUrlsAsync(client, cancellationToken);
        if (subSitemaps.Count == 0)
        {
            _logger.LogWarning("No sub-sitemaps discovered from TopDev sitemap index. Nothing to scrape.");
            yield break;
        }

        // Sub-sitemaps are ordered newest-first (jobs_desc_*), so scanning from
        // the top and skipping anything already in the DB naturally collects the
        // freshest postings we don't have yet. MaxSitemapPages is only a safety
        // cap on how deep we scan; the real target is MaxJobsPerRun NEW jobs.
        var maxSitemaps = Math.Max(1, _options.MaxSitemapPages);
        var sitemapsToProcess = subSitemaps.Take(maxSitemaps).ToList();
        _logger.LogInformation(
            "Discovered {Total} sub-sitemaps; scanning up to {Count} (newest first) for {Target} new jobs.",
            subSitemaps.Count, sitemapsToProcess.Count, _options.MaxJobsPerRun);

        var pagesScanned = 0;
        var skippedExisting = 0;
        foreach (var sitemapUrl in sitemapsToProcess)
        {
            if (cancellationToken.IsCancellationRequested || yielded >= _options.MaxJobsPerRun)
            {
                break;
            }

            pagesScanned++;
            var jobUrls = await GetJobUrlsAsync(client, sitemapUrl, cancellationToken);
            _logger.LogInformation("Sub-sitemap {Url} yielded {Count} job URLs.", sitemapUrl, jobUrls.Count);

            foreach (var jobUrl in jobUrls)
            {
                if (cancellationToken.IsCancellationRequested || yielded >= _options.MaxJobsPerRun)
                {
                    break;
                }

                var externalId = ExtractExternalId(jobUrl);
                if (string.IsNullOrWhiteSpace(externalId) || !seenIds.Add(externalId))
                {
                    continue;
                }

                if (!_options.RefreshExistingJobs && existingExternalIds.Contains(externalId))
                {
                    skippedExisting++;
                    _logger.LogDebug("Skipping existing TopDev job {ExternalId}.", externalId);
                    continue;
                }

                await DelayBeforeDetailRequestAsync(cancellationToken);

                // All exception handling lives in the helper so the iterator body
                // stays free of try/catch (C# forbids yield inside try-catch).
                var scraped = await TryScrapeDetailAsync(client, jobUrl, externalId, cancellationToken);
                if (scraped is not null)
                {
                    yielded++;
                    yield return scraped;
                }
            }
        }

        _logger.LogInformation(
            "TopDev scrape finished: {Yielded} new job(s) collected, {Skipped} already-known skipped, "
            + "across {Pages} sub-sitemap(s) scanned.",
            yielded, skippedExisting, pagesScanned);

        if (yielded < _options.MaxJobsPerRun)
        {
            _logger.LogInformation(
                "Collected fewer than the {Target} target new jobs; either TopDev has no more fresh "
                + "postings within the scanned pages or MaxSitemapPages ({Cap}) is too low.",
                _options.MaxJobsPerRun, maxSitemaps);
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.RequestTimeoutSeconds));
        return client;
    }

    private async Task DelayBeforeDetailRequestAsync(CancellationToken cancellationToken)
    {
        var minDelay = Math.Max(0, _options.MinRequestDelayMs);
        var maxDelay = Math.Max(minDelay, _options.MaxRequestDelayMs);
        if (maxDelay == 0)
        {
            return;
        }

        var delay = Random.Shared.Next(minDelay, maxDelay + 1);
        _logger.LogDebug("Waiting {DelayMs}ms before next TopDev detail request.", delay);
        await Task.Delay(delay, cancellationToken);
    }

    private async Task<ScrapedJob?> TryScrapeDetailAsync(
        HttpClient client,
        string jobUrl,
        string externalId,
        CancellationToken cancellationToken)
    {
        try
        {
            var html = await FetchStringAsync(client, jobUrl, cancellationToken);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            return ParseJobDetail(html, jobUrl, externalId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to scrape TopDev job {Url}.", jobUrl);
            return null;
        }
    }

    private async Task<List<string>> GetSubSitemapUrlsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var indexUrl = $"{_options.BaseUrl.TrimEnd('/')}{_options.SitemapIndexPath}";
        var xml = await FetchStringAsync(client, indexUrl, cancellationToken);
        if (string.IsNullOrEmpty(xml))
        {
            return new List<string>();
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to parse TopDev sitemap index at {Url}.", indexUrl);
            return new List<string>();
        }

        // If the configured path already returns a urlset (job URLs directly),
        // there is no index level to expand; fetch job URLs from it directly.
        if (string.Equals(document.Root?.Name.LocalName, "urlset", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { indexUrl };
        }

        return document.Descendants()
            .Where(element => element.Name.LocalName == "loc")
            .Select(element => element.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private async Task<List<string>> GetJobUrlsAsync(HttpClient client, string sitemapUrl, CancellationToken cancellationToken)
    {
        var urls = new List<string>();
        var xml = await FetchStringAsync(client, sitemapUrl, cancellationToken);
        if (string.IsNullOrEmpty(xml))
        {
            return urls;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to parse TopDev sub-sitemap at {Url}.", sitemapUrl);
            return urls;
        }

        foreach (var loc in document.Descendants().Where(element => element.Name.LocalName == "loc"))
        {
            var value = loc.Value.Trim();
            if (value.Contains("/detail-jobs/", StringComparison.OrdinalIgnoreCase))
            {
                urls.Add(value);
            }
        }

        return urls;
    }

    private async Task<string?> FetchStringAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
            request.Headers.TryAddWithoutValidation(
                "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation(
                "Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");

            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var status = (int)response.StatusCode;
            if (status is 403 or 429 or 503)
            {
                _logger.LogWarning("TopDev blocked request ({Status}) for {Url}.", status, url);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TopDev fetch {Url} returned status {Status}.", url, status);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error fetching {Url} from TopDev.", url);
            return null;
        }
    }

    private static string ExtractExternalId(string url)
    {
        var path = url;
        var queryIndex = path.IndexOfAny(new[] { '?', '#' });
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        path = path.TrimEnd('/');
        var match = ExternalIdFromUrl.Match(path);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private ScrapedJob? ParseJobDetail(string html, string url, string externalId)
    {
        var posting = ExtractJobPostingElement(html);
        if (posting is null)
        {
            _logger.LogDebug("No JobPosting JSON-LD found for {Url}.", url);
            return null;
        }

        var element = posting.Value;

        var title = GetString(element, "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogDebug("JobPosting JSON-LD without title for {Url}; skipping.", url);
            return null;
        }

        // TopDev's all-industry sitemap mixes in non-IT postings (banking, sales,
        // etc.) yet tags every one as industry "Information Technology", so that
        // field is useless. The "skills" field is the reliable signal; combine it
        // with the title to decide whether this is genuinely an IT role. This is
        // an IT-only job board, so anything that doesn't look like IT is dropped.
        var skills = GetString(element, "skills");
        if (!_skillExtractor.LooksLikeItJob(title, skills))
        {
            _logger.LogDebug("Skipping non-IT TopDev job {Url} (skills: {Skills}).", url, skills);
            return null;
        }

        var company = GetString(GetProperty(element, "hiringOrganization"), "name");
        var location = ExtractLocation(element);
        var (salaryText, salaryMin, salaryMax) = ExtractSalary(element);
        var description = StripHtml(GetString(element, "description"));
        var postedAt = ParseDate(GetString(element, "datePosted"));

        return new ScrapedJob
        {
            ExternalId = externalId,
            Title = NormalizeWhitespace(WebUtility.HtmlDecode(title))!,
            CompanyName = NormalizeWhitespace(WebUtility.HtmlDecode(company)),
            Location = NormalizeWhitespace(WebUtility.HtmlDecode(location)),
            SalaryText = salaryText,
            SalaryMinMillionVnd = salaryMin,
            SalaryMaxMillionVnd = salaryMax,
            Description = description,
            SourceUrl = url,
            PostedAt = postedAt,
        };
    }

    /// <summary>
    /// Finds the schema.org JobPosting object among the page's JSON-LD blocks.
    /// Handles a bare object, an array of objects, and an "@graph" wrapper.
    /// The returned element is cloned so it stays valid after the backing
    /// <see cref="JsonDocument"/> is disposed.
    /// </summary>
    private static JsonElement? ExtractJobPostingElement(string html)
    {
        foreach (Match match in LdJsonScript.Matches(html))
        {
            var raw = match.Groups[1].Value.Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(raw);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var found = FindJobPosting(document.RootElement);
                if (found is not null)
                {
                    return found.Value.Clone();
                }
            }
        }

        return null;
    }

    private static JsonElement? FindJobPosting(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (IsJobPosting(element))
                {
                    return element;
                }

                if (element.TryGetProperty("@graph", out var graph))
                {
                    return FindJobPosting(graph);
                }

                return null;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindJobPosting(item);
                    if (found is not null)
                    {
                        return found;
                    }
                }

                return null;

            default:
                return null;
        }
    }

    private static bool IsJobPosting(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
        {
            return false;
        }

        return type.ValueKind switch
        {
            JsonValueKind.String =>
                string.Equals(type.GetString(), "JobPosting", StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Array => type.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), "JobPosting", StringComparison.OrdinalIgnoreCase)),
            _ => false,
        };
    }

    private static string? ExtractLocation(JsonElement element)
    {
        if (!element.TryGetProperty("jobLocation", out var jobLocation))
        {
            return null;
        }

        var target = jobLocation;
        if (jobLocation.ValueKind == JsonValueKind.Array)
        {
            if (jobLocation.GetArrayLength() == 0)
            {
                return null;
            }

            target = jobLocation[0];
        }

        if (target.ValueKind != JsonValueKind.Object
            || !target.TryGetProperty("address", out var address)
            || address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var key in new[] { "addressRegion", "addressLocality", "streetAddress" })
        {
            if (address.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        return null;
    }

    private (string? Text, decimal? Min, decimal? Max) ExtractSalary(JsonElement element)
    {
        if (!element.TryGetProperty("baseSalary", out var baseSalary)
            || baseSalary.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null);
        }

        var currency = baseSalary.TryGetProperty("currency", out var currencyElement)
            && currencyElement.ValueKind == JsonValueKind.String
            ? currencyElement.GetString()
            : "VND";

        if (!baseSalary.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null);
        }

        string? text = null;
        if (value.TryGetProperty("value", out var textValue) && textValue.ValueKind == JsonValueKind.String)
        {
            text = NormalizeWhitespace(textValue.GetString());
        }

        var min = ToMillion(ReadDecimal(value, "minValue"), currency);
        var max = ToMillion(ReadDecimal(value, "maxValue"), currency);

        if (string.IsNullOrWhiteSpace(text) && (min.HasValue || max.HasValue))
        {
            text = BuildSalaryText(min, max);
        }

        return (string.IsNullOrWhiteSpace(text) ? null : text, min, max);
    }

    private static decimal? ReadDecimal(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String => ParseDecimal(value.GetString()),
            _ => null,
        };
    }

    private static decimal? ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Keep digits and a single decimal separator; drop currency words/spaces.
        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        cleaned = cleaned.Replace(",", string.Empty);
        var lastDot = cleaned.LastIndexOf('.');
        if (lastDot >= 0)
        {
            // Treat dots as thousand separators (VND style) by removing them.
            cleaned = cleaned.Replace(".", string.Empty);
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static decimal? ToMillion(decimal? raw, string? currency)
    {
        if (!raw.HasValue || raw.Value <= 0)
        {
            return null;
        }

        var vnd = string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? raw.Value * UsdToVnd
            : raw.Value;

        return Math.Round(vnd / 1_000_000m, 2);
    }

    private static string BuildSalaryText(decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue)
        {
            return $"{FormatMillion(min.Value)} - {FormatMillion(max.Value)} triệu";
        }

        if (max.HasValue)
        {
            return $"Tới {FormatMillion(max.Value)} triệu";
        }

        return min.HasValue ? $"Từ {FormatMillion(min.Value)} triệu" : string.Empty;
    }

    private static string FormatMillion(decimal value)
    {
        // Drop trailing ".00" for whole numbers.
        return value == Math.Truncate(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var builder = new StringBuilder();
        foreach (var node in document.DocumentNode.DescendantsAndSelf())
        {
            if (node.NodeType != HtmlNodeType.Text)
            {
                continue;
            }

            var text = WebUtility.HtmlDecode(node.InnerText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.Append(text.Trim()).Append('\n');
            }
        }

        var result = NormalizeLines(builder.ToString());
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? GetString(JsonElement element, string propertyName)
        => GetString((JsonElement?)element, propertyName);

    private static string? GetString(JsonElement? element, string propertyName)
    {
        var property = GetProperty(element, propertyName);
        if (property is null)
        {
            return null;
        }

        return property.Value.ValueKind switch
        {
            JsonValueKind.String => property.Value.GetString(),
            JsonValueKind.Number => property.Value.GetRawText(),
            _ => null,
        };
    }

    private static JsonElement? GetProperty(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.Value.TryGetProperty(propertyName, out var value) ? value : null;
    }

    private static string? NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    private static string NormalizeLines(string text)
    {
        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0);
        return string.Join('\n', lines).Trim();
    }
}
