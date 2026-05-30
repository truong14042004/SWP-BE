using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class TopCvScraper : IJobScraper
{
    public string SourceName => "TopCV";

    private readonly TopCvScraperOptions _options;
    private readonly ILogger<TopCvScraper> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private bool _blocked;
    private static string? _cachedProxy;
    private static string? _cachedOutboundIp;

    private static readonly Regex JobIdFromUrl = new(@"/viec-lam/[^/]+/(\d+)\.html", RegexOptions.Compiled);
    private static readonly Regex JobUrlCandidate = new(
        @"(?:https?:)?(?://www\.topcv\.vn)?/viec-lam/[^""'\\\s<>]+/\d+\.html(?:\?[^""'\\\s<>]*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SalaryRange = new(@"(\d+(?:[.,]\d+)?)\s*-\s*(\d+(?:[.,]\d+)?)\s*(triệu|tr|million|usd)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SalaryUpTo = new(@"(?:tới|đến|up to)\s*(\d+(?:[.,]\d+)?)\s*(triệu|tr|million|usd)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TopCvScraper(
        IOptions<MarketPulseOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TopCvScraper> logger)
    {
        _options = options.Value.TopCv;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TopCV scraper is disabled via configuration.");
            yield break;
        }

        _blocked = false;
        var seenIds = new HashSet<string>();
        var yielded = 0;
        var proxyRotations = 0;
        var maxProxyRotations = Math.Max(0, _options.MaxProxyRotationsPerRun);

        _logger.LogInformation("Starting TopCV scrape run using Playwright.");

        using var playwright = await Playwright.CreateAsync();

        var browser = await CreateBrowserAsync(playwright, await ResolveProxyAsync(forceNew: true, cancellationToken));
        var (context, page) = await CreateContextAsync(browser);

        var consecutiveBlocks = 0;
        try
        {
            for (var pageNum = 1; pageNum <= _options.MaxPages; pageNum++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                var listUrl = $"{_options.BaseUrl.TrimEnd('/')}{_options.ListPath}?page={pageNum}";
                HtmlDocument? listDocument = null;
                var listAttempt = 0;

                while (true)
                {
                    listAttempt++;

                    try
                    {
                        _blocked = false;
                        listDocument = await FetchAsync(page, listUrl, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Failed to fetch TopCV list page {Page}.", pageNum);
                    }

                    if (listDocument is not null)
                    {
                        break;
                    }

                    if (!_blocked || proxyRotations >= maxProxyRotations)
                    {
                        break;
                    }

                    proxyRotations++;
                    _logger.LogInformation(
                        "List page {Page} blocked/network-failed; rotating proxy (attempt {N}/{Max})...",
                        pageNum, proxyRotations, maxProxyRotations);

                    await Task.Delay(15_000, cancellationToken);

                    await context.DisposeAsync();
                    await browser.DisposeAsync();

                    browser = await CreateBrowserAsync(
                        playwright,
                        await ResolveProxyAsync(forceNew: true, cancellationToken));
                    (context, page) = await CreateContextAsync(browser);

                    consecutiveBlocks = 0;
                }

                if (listDocument is null)
                {
                    break;
                }

                var jobUrls = ExtractJobUrls(listDocument);
                if (jobUrls.Count == 0)
                {
                    var pageTitle = SelectText(listDocument, "//title") ?? "(no title)";
                    var linkCount = listDocument.DocumentNode.SelectNodes("//a[@href]")?.Count ?? 0;
                    var htmlLength = listDocument.DocumentNode.OuterHtml.Length;
                    _logger.LogInformation(
                        "No job URLs found on page {Page}. Stopping pagination. Page title: {Title}. Link count: {LinkCount}. Html length: {HtmlLength}.",
                        pageNum,
                        pageTitle,
                        linkCount,
                        htmlLength);
                    break;
                }

                foreach (var jobUrl in jobUrls)
                {
                    if (cancellationToken.IsCancellationRequested || yielded >= _options.MaxJobsPerRun)
                    {
                        yield break;
                    }

                    var externalId = ExtractExternalId(jobUrl);
                    if (string.IsNullOrWhiteSpace(externalId) || !seenIds.Add(externalId))
                    {
                        continue;
                    }

                    if (!_options.RefreshExistingJobs && existingExternalIds.Contains(externalId))
                    {
                        _logger.LogDebug("Skipping existing TopCV job {ExternalId} without opening detail page.", externalId);
                        continue;
                    }

                    ScrapedJob? scraped = null;
                    try
                    {
                        // Randomized delay (±30%) to appear more human-like
                        await DelayBeforeDetailRequestAsync(cancellationToken);

                        _blocked = false;
                        var detailDocument = await FetchAsync(page, jobUrl, cancellationToken);

                        if (detailDocument is null && _blocked)
                        {
                            consecutiveBlocks++;
                            _logger.LogWarning(
                                "Detail page blocked (#{Count}): {Url}", consecutiveBlocks, jobUrl);

                            if (consecutiveBlocks >= 3)
                            {
                                if (proxyRotations >= maxProxyRotations)
                                {
                                    _logger.LogWarning(
                                        "Exhausted {Max} proxy rotations. Stopping scrape run.", maxProxyRotations);
                                    yield break;
                                }

                                proxyRotations++;
                                _logger.LogInformation(
                                    "Rotating proxy (attempt {N}/{Max})...", proxyRotations, maxProxyRotations);

                                await Task.Delay(15_000, cancellationToken);

                                await context.DisposeAsync();
                                await browser.DisposeAsync();

                                browser = await CreateBrowserAsync(
                                    playwright,
                                    await ResolveProxyAsync(forceNew: true, cancellationToken));
                                (context, page) = await CreateContextAsync(browser);

                                consecutiveBlocks = 0;
                            }

                            continue;
                        }

                        if (detailDocument is null)
                        {
                            continue;
                        }

                        consecutiveBlocks = 0;
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
        finally
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
        }
    }

    private async Task DelayBeforeDetailRequestAsync(CancellationToken cancellationToken)
    {
        var minDelay = Math.Max(3000, _options.MinRequestDelayMs);
        var maxDelay = Math.Max(minDelay, _options.MaxRequestDelayMs);
        var delay = Random.Shared.Next(minDelay, maxDelay + 1);

        _logger.LogDebug("Waiting {DelayMs}ms before next TopCV detail request.", delay);
        await Task.Delay(delay, cancellationToken);
    }
    private async Task<IBrowser> CreateBrowserAsync(IPlaywright playwright, string? proxyServer)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"]
        };

        if (!string.IsNullOrWhiteSpace(proxyServer))
        {
            _logger.LogInformation("Using proxy: {ProxyServer}", proxyServer);
            launchOptions.Proxy = new Proxy { Server = proxyServer };
        }

        return await playwright.Chromium.LaunchAsync(launchOptions);
    }

    private static async Task<(IBrowserContext context, IPage page)> CreateContextAsync(IBrowser browser)
    {
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale = "vi-VN",
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept-Language", "vi-VN,vi;q=0.9,en-US;q=0.8,en;q=0.7" }
            },
            JavaScriptEnabled = false
        });

        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            window.chrome = { runtime: {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['vi-VN', 'vi', 'en-US', 'en'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
        ");

        var page = await context.NewPageAsync();
        return (context, page);
    }

    private async Task<string?> ResolveProxyAsync(bool forceNew, CancellationToken cancellationToken)
    {
        string? resolved = null;

        if (_options.UseDynamicProxy && !string.IsNullOrWhiteSpace(_options.ProxyKey))
        {
            try
            {
                resolved = await GetDynamicProxyAsync(forceNew, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch dynamic proxy. Trying static fallback proxy.");
            }
        }

        if (string.IsNullOrWhiteSpace(resolved) && !string.IsNullOrWhiteSpace(_options.ProxyServer))
        {
            resolved = _options.ProxyServer;
        }

        return resolved;
    }

    private async Task<HtmlDocument?> FetchAsync(IPage page, string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.RequestTimeoutSeconds * 1000
            });

            if (response == null)
            {
                return null;
            }

            if (response.Status == 404)
            {
                return null;
            }

            if (response.Status is 403 or 429 or 503)
            {
                _blocked = true;
                _logger.LogWarning("TopCV blocked request ({StatusCode}) for {Url}. Stopping scrape run.",
                    response.Status, url);
                return null;
            }

            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = Math.Min(_options.RequestTimeoutSeconds * 1000, 10000)
                });
            }
            catch (TimeoutException)
            {
                _logger.LogDebug("Timed out waiting for network idle on {Url}; continuing with current DOM.", url);
            }

            // Give client-side rendering one more beat before reading the DOM.
            await Task.Delay(2000, cancellationToken);

            var title = await page.TitleAsync();
            if (title.Contains("Cloudflare") || title.Contains("Just a moment"))
            {
                _blocked = true;
                _logger.LogWarning("TopCV blocked request (Cloudflare challenge detected) for {Url}. Stopping scrape run.",
                    url);
                return null;
            }

            if (!response.Ok)
            {
                _logger.LogWarning("Failed to fetch {Url} with status {Status}.", url, response.Status);
                return null;
            }

            var html = await page.ContentAsync();
            var document = new HtmlDocument();
            document.LoadHtml(html);
            return document;
        }
        catch (Exception exception)
        {
            if (IsTransientNetworkFailure(exception))
            {
                _blocked = true;
            }

            _logger.LogWarning(exception, "Error while navigating to {Url}.", url);
            return null;
        }
    }

    private static bool IsTransientNetworkFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (string.IsNullOrEmpty(message))
            {
                continue;
            }

            if (message.Contains("ERR_CONNECTION_", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_TUNNEL_", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_PROXY_", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_TIMED_OUT", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_NETWORK_", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_NAME_NOT_RESOLVED", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_SOCKS_", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ERR_EMPTY_RESPONSE", StringComparison.OrdinalIgnoreCase)
                || message.Contains("net::ERR_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return exception is TimeoutException;
    }

    private static List<string> ExtractJobUrls(HtmlDocument document)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var links = document.DocumentNode.SelectNodes("//a[contains(@href,'/viec-lam/')]");
        if (links is not null)
        {
            foreach (var link in links)
            {
                AddJobUrlCandidate(urls, link.GetAttributeValue("href", string.Empty));
            }
        }

        var html = document.DocumentNode.OuterHtml;
        foreach (Match match in JobUrlCandidate.Matches(html))
        {
            AddJobUrlCandidate(urls, match.Value);
        }

        var decodedHtml = WebUtility.HtmlDecode(html).Replace("\\/", "/", StringComparison.Ordinal);
        if (!string.Equals(decodedHtml, html, StringComparison.Ordinal))
        {
            foreach (Match match in JobUrlCandidate.Matches(decodedHtml))
            {
                AddJobUrlCandidate(urls, match.Value);
            }
        }

        return urls.ToList();
    }

    private static void AddJobUrlCandidate(HashSet<string> urls, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        var decodedHref = WebUtility.HtmlDecode(candidate)
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Trim();

        if (!JobIdFromUrl.IsMatch(decodedHref))
        {
            return;
        }

        string absolute;
        if (decodedHref.StartsWith("//", StringComparison.Ordinal))
        {
            absolute = $"https:{decodedHref}";
        }
        else if (decodedHref.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            absolute = decodedHref;
        }
        else if (decodedHref.StartsWith("/", StringComparison.Ordinal))
        {
            absolute = $"https://www.topcv.vn{decodedHref}";
        }
        else
        {
            return;
        }

        // Strip tracking/session parameters (ta_source, u_sr_id, etc.)
        // These are session tokens tied to the scraping session IP, causing Cloudflare
        // to detect the bot when the same token is reused with a different connection.
        var cleanUrl = StripTrackingParams(absolute);
        urls.Add(cleanUrl);
    }

    private static string StripTrackingParams(string url)
    {
        try
        {
            var uri = new Uri(url);
            // Keep only the path — drop all query string parameters
            // (ta_source, u_sr_id and similar are session/tracking tokens)
            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
        }
        catch
        {
            return url;
        }
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

    private async Task<string?> GetDynamicProxyAsync(bool forceNew, CancellationToken cancellationToken)
    {
        if (!forceNew && !string.IsNullOrWhiteSpace(_cachedProxy))
        {
            return _cachedProxy;
        }

        var client = _httpClientFactory.CreateClient();

        var getUrl = $"{_options.ProxyApiUrl}?key={_options.ProxyKey}&nhamang={_options.ProxyNetwork}&tinhthanh={_options.ProxyLocation}";
        if (!string.IsNullOrWhiteSpace(_options.ProxyWhitelist))
        {
            getUrl += $"&whitelist={_options.ProxyWhitelist}";
        }

        if (!forceNew)
        {
            try
            {
                var direct = await client.GetFromJsonAsync<ProxyXoayResponse>(getUrl, cancellationToken);
                if (direct?.Status == 100 && !string.IsNullOrWhiteSpace(direct.ProxyHttp))
                {
                    var clean = CleanProxyUrl(direct.ProxyHttp);
                    if (!string.IsNullOrWhiteSpace(clean))
                    {
                        _cachedProxy = clean;
                        if (!string.IsNullOrWhiteSpace(direct.Ip))
                        {
                            _cachedOutboundIp = direct.Ip;
                        }
                        _logger.LogInformation(
                            "Reusing current dynamic proxy: {Proxy} (outbound {Ip}).",
                            clean, direct.Ip ?? "?");
                        return clean;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while calling ProxyXoay API (initial).");
            }
        }

        var previousIp = _cachedOutboundIp;
        if (forceNew && !string.IsNullOrWhiteSpace(_options.ProxyRotateUrl))
        {
            _logger.LogInformation(
                "Forcing proxy IP rotation via API (previous outbound {Ip}).",
                previousIp ?? "?");
            try
            {
                var rotateUrl = $"{_options.ProxyRotateUrl}?key={_options.ProxyKey}";
                var rotateResponse = await client.GetAsync(rotateUrl, cancellationToken);
                var responseContent = await rotateResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation(
                    "Proxy rotation HTTP {Status}: {Response}",
                    (int)rotateResponse.StatusCode,
                    string.IsNullOrWhiteSpace(responseContent) ? "<empty>" : responseContent.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to force proxy rotation via API.");
            }
        }

        const int pollDelayMs = 5000;
        const int maxWaitMs = 90_000;
        var elapsed = 0;
        ProxyXoayResponse? lastResponse = null;
        while (elapsed < maxWaitMs)
        {
            try
            {
                lastResponse = await client.GetFromJsonAsync<ProxyXoayResponse>(getUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while polling ProxyXoay API at {Elapsed}ms.", elapsed);
                lastResponse = null;
            }

            if (lastResponse?.Status == 100 && !string.IsNullOrWhiteSpace(lastResponse.ProxyHttp))
            {
                if (forceNew
                    && !string.IsNullOrWhiteSpace(previousIp)
                    && !string.IsNullOrWhiteSpace(lastResponse.Ip)
                    && string.Equals(previousIp, lastResponse.Ip, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "ProxyXoay still returns previous outbound {Ip} after {Elapsed}ms; waiting...",
                        lastResponse.Ip, elapsed);
                }
                else
                {
                    var cleanProxy = CleanProxyUrl(lastResponse.ProxyHttp);
                    if (!string.IsNullOrWhiteSpace(cleanProxy))
                    {
                        _cachedProxy = cleanProxy;
                        if (!string.IsNullOrWhiteSpace(lastResponse.Ip))
                        {
                            _cachedOutboundIp = lastResponse.Ip;
                        }
                        _logger.LogInformation(
                            "Acquired dynamic proxy {Proxy} (outbound {Ip}) after {Elapsed}ms.",
                            cleanProxy, lastResponse.Ip ?? "?", elapsed);
                        return cleanProxy;
                    }
                }
            }
            else if (lastResponse?.Status == 101)
            {
                _logger.LogInformation(
                    "ProxyXoay cooldown at {Elapsed}ms: {Message}",
                    elapsed, lastResponse.Message);
            }
            else if (lastResponse is not null)
            {
                _logger.LogWarning(
                    "ProxyXoay returned status {Status} at {Elapsed}ms: {Message}",
                    lastResponse.Status, elapsed, lastResponse.Message);
            }

            await Task.Delay(pollDelayMs, cancellationToken);
            elapsed += pollDelayMs;
        }

        _logger.LogWarning(
            "Gave up waiting for ProxyXoay rotation after {Elapsed}ms; falling back to last known proxy {Proxy}.",
            elapsed, _cachedProxy ?? "<none>");
        return _cachedProxy;
    }

    private static string? CleanProxyUrl(string rawProxy)
    {
        // e.g., "160.250.166.38:10413::" -> "http://160.250.166.38:10413"
        var parts = rawProxy.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"http://{parts[0]}:{parts[1]}";
        }
        return null;
    }
}

public sealed class ProxyXoayResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("proxyhttp")]
    public string? ProxyHttp { get; set; }

    [JsonPropertyName("proxysocks5")]
    public string? ProxySocks5 { get; set; }

    [JsonPropertyName("ip")]
    public string? Ip { get; set; }
}
