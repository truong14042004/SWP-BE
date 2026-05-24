using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class TopCvScraper : IJobScraper
{
    public string SourceName => "TopCV";

    private readonly HttpClient _httpClient;
    private readonly TopCvScraperOptions _options;
    private readonly ILogger<TopCvScraper> _logger;
    private bool _blocked;

    private static readonly Regex JobIdFromUrl = new(@"/viec-lam/[^/]+/(\d+)\.html", RegexOptions.Compiled);
    private static readonly Regex SalaryRange = new(@"(\d+(?:[.,]\d+)?)\s*-\s*(\d+(?:[.,]\d+)?)\s*(triệu|tr|million|usd)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SalaryUpTo = new(@"(?:tới|đến|up to)\s*(\d+(?:[.,]\d+)?)\s*(triệu|tr|million|usd)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TopCvScraper(HttpClient httpClient, IOptions<MarketPulseOptions> options, ILogger<TopCvScraper> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.TopCv;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
        if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(_options.UserAgent))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
        }
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TopCV scraper is disabled via configuration.");
            yield break;
        }

        _blocked = false;
        var seenIds = new HashSet<string>();
        var yielded = 0;

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            if (_blocked)
            {
                yield break;
            }

            var listUrl = $"{_options.BaseUrl.TrimEnd('/')}{_options.ListPath}?page={page}";
            HtmlDocument? listDocument;

            try
            {
                listDocument = await FetchAsync(listUrl, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to fetch TopCV list page {Page}.", page);
                break;
            }

            if (listDocument is null)
            {
                break;
            }

            var jobUrls = ExtractJobUrls(listDocument);
            if (jobUrls.Count == 0)
            {
                _logger.LogInformation("No job URLs found on page {Page}. Stopping pagination.", page);
                break;
            }

            foreach (var jobUrl in jobUrls)
            {
                if (cancellationToken.IsCancellationRequested || yielded >= _options.MaxJobsPerRun || _blocked)
                {
                    yield break;
                }

                var externalId = ExtractExternalId(jobUrl);
                if (string.IsNullOrWhiteSpace(externalId) || !seenIds.Add(externalId))
                {
                    continue;
                }

                ScrapedJob? scraped = null;
                try
                {
                    await Task.Delay(_options.RequestDelayMs, cancellationToken);
                    var detailDocument = await FetchAsync(jobUrl, cancellationToken);
                    if (detailDocument is null)
                    {
                        if (_blocked)
                        {
                            yield break;
                        }
                        continue;
                    }
                    scraped = ParseJobDetail(detailDocument, jobUrl, externalId);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to scrape TopCV job {Url}.", jobUrl);
                }

                if (scraped is not null)
                {
                    yielded++;
                    yield return scraped;
                }
            }
        }
    }

    private async Task<HtmlDocument?> FetchAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (response.StatusCode is HttpStatusCode.Forbidden
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable)
        {
            _blocked = true;
            _logger.LogWarning("TopCV blocked request ({StatusCode}) for {Url}. Stopping scrape run.",
                (int)response.StatusCode, url);
            return null;
        }
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = new HtmlDocument();
        document.LoadHtml(html);
        return document;
    }

    private static List<string> ExtractJobUrls(HtmlDocument document)
    {
        var links = document.DocumentNode
            .SelectNodes("//a[contains(@href,'/viec-lam/')]")
            ?? new HtmlNodeCollection(null);

        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (!JobIdFromUrl.IsMatch(href))
            {
                continue;
            }

            var absolute = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : $"https://www.topcv.vn{href}";
            urls.Add(absolute);
        }

        return urls.ToList();
    }

    private static string ExtractExternalId(string url)
    {
        var match = JobIdFromUrl.Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private ScrapedJob? ParseJobDetail(HtmlDocument document, string url, string externalId)
    {
        var title = SelectText(document,
            "//h1[contains(@class,'job-detail__info--title')]",
            "//h1",
            "//div[contains(@class,'job-title')]");
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var company = SelectText(document,
            "//a[contains(@class,'company-name')]",
            "//div[contains(@class,'company-name-label')]",
            "//h2[contains(@class,'company-name')]",
            "//*[contains(@class,'company') and contains(@class,'name')]");

        var labelValues = ExtractInfoSectionPairs(document);
        var location = FindByLabel(labelValues, "địa điểm", "nơi làm", "địa chỉ");
        var salary = FindByLabel(labelValues, "mức lương", "lương")
            ?? SelectText(document, "//*[contains(@class,'job-detail__info--section-content-value')][contains(translate(., 'TRIỆU', 'triệu'),'triệu') or contains(., '$')]");

        var descriptionNode = document.DocumentNode.SelectSingleNode(
            "//div[contains(@class,'job-description')]")
            ?? document.DocumentNode.SelectSingleNode("//section[contains(@class,'job-description')]")
            ?? document.DocumentNode.SelectSingleNode("//div[contains(@class,'job-data')]");

        var description = descriptionNode is null
            ? null
            : WebUtility.HtmlDecode(descriptionNode.InnerText)?.Trim();

        var (minM, maxM) = ParseSalaryRange(salary);

        return new ScrapedJob
        {
            ExternalId = externalId,
            Title = title!,
            CompanyName = company,
            Location = location,
            SalaryText = salary,
            SalaryMinMillionVnd = minM,
            SalaryMaxMillionVnd = maxM,
            Description = description,
            SourceUrl = url,
            PostedAt = null,
        };
    }

    private static Dictionary<string, string> ExtractInfoSectionPairs(HtmlDocument document)
    {
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var titleNodes = document.DocumentNode.SelectNodes(
            "//*[contains(@class,'job-detail__info--section-content-title') or contains(@class,'job-detail__info-section-title')]");
        if (titleNodes is null)
        {
            return pairs;
        }

        foreach (var titleNode in titleNodes)
        {
            var label = NormalizeWhitespace(WebUtility.HtmlDecode(titleNode.InnerText));
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            HtmlNode? valueNode = null;
            var parent = titleNode.ParentNode;
            if (parent is not null)
            {
                valueNode = parent.SelectSingleNode(".//*[contains(@class,'job-detail__info--section-content-value') or contains(@class,'job-detail__info-section-value') or contains(@class,'value')]");
            }
            valueNode ??= titleNode.SelectSingleNode("following-sibling::*[1]");
            if (valueNode is null)
            {
                continue;
            }

            var value = NormalizeWhitespace(WebUtility.HtmlDecode(valueNode.InnerText));
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, label, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            pairs[label] = value;
        }

        return pairs;
    }

    private static string? FindByLabel(Dictionary<string, string> pairs, params string[] labelHints)
    {
        foreach (var hint in labelHints)
        {
            var match = pairs
                .Where(pair => pair.Key.Contains(hint, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Value)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }
        }
        return null;
    }

    private static string NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    private static string? SelectText(HtmlDocument document, params string[] xpaths)
    {
        foreach (var xpath in xpaths)
        {
            var node = document.DocumentNode.SelectSingleNode(xpath);
            if (node is null)
            {
                continue;
            }
            var text = WebUtility.HtmlDecode(node.InnerText)?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return Regex.Replace(text, @"\s+", " ");
            }
        }
        return null;
    }

    private static (decimal? Min, decimal? Max) ParseSalaryRange(string? salary)
    {
        if (string.IsNullOrWhiteSpace(salary))
        {
            return (null, null);
        }

        var match = SalaryRange.Match(salary);
        if (match.Success)
        {
            var min = ParseDecimal(match.Groups[1].Value);
            var max = ParseDecimal(match.Groups[2].Value);
            var unit = match.Groups[3].Value.ToLowerInvariant();
            return unit is "usd" ? (min * 25, max * 25) : (min, max);
        }

        var upTo = SalaryUpTo.Match(salary);
        if (upTo.Success)
        {
            var max = ParseDecimal(upTo.Groups[1].Value);
            var unit = upTo.Groups[2].Value.ToLowerInvariant();
            return unit is "usd" ? (null, max * 25) : (null, max);
        }

        return (null, null);
    }

    private static decimal? ParseDecimal(string raw)
    {
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
