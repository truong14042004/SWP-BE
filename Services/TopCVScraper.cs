using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SWP_BE.Models;
using SWP_BE.Options;

namespace SWP_BE.Services;

public class TopCVScraper : IJobScraper
{
    public string SourceName => "TopCV";
    
    private readonly ILogger<TopCVScraper> _logger;
    private readonly HttpClient _httpClient;
    private readonly TopCVScraperOptions _options;

    public TopCVScraper(
        ILogger<TopCVScraper> logger,
        HttpClient httpClient,
        IOptions<MarketPulseOptions> marketPulseOptions)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = marketPulseOptions.Value.TopCV;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TopCVScraper bị tắt trong cấu hình.");
            yield break;
        }

        var apiKey = Environment.GetEnvironmentVariable("ZENROWS_API_KEY") ?? _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Không tìm thấy ZENROWS_API_KEY cho TopCVScraper. Bỏ qua cào TopCV.");
            yield break;
        }

        _logger.LogInformation("Bắt đầu cào TopCV bằng ZenRows API + HtmlAgilityPack...");

        int jobsFound = 0;

        for (int pageNum = 1; pageNum <= _options.MaxPages; pageNum++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var targetUrl = _options.BaseUrl.Contains('?') 
                ? $"{_options.BaseUrl}&page={pageNum}" 
                : $"{_options.BaseUrl}?page={pageNum}";
            var encodedTarget = Uri.EscapeDataString(targetUrl);
            var zenRowsUrl = $"https://api.zenrows.com/v1/?apikey={apiKey}&url={encodedTarget}" +
                             $"&js_render={_options.JsRender.ToString().ToLower()}" +
                             $"&premium_proxy={_options.PremiumProxy.ToString().ToLower()}" +
                             $"&wait_for=.job-item-search-result";

            _logger.LogInformation("TopCV: Đang gọi ZenRows lấy HTML trang {TargetUrl}", targetUrl);
            
            string htmlContent = "";
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(120)); // ZenRows API render JS có thể mất lâu

                var response = await _httpClient.GetAsync(zenRowsUrl, cts.Token);
                response.EnsureSuccessStatusCode();
                htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi ZenRows API khi cào TopCV trang {PageNum}.", pageNum);
                break; // Thất bại thì dừng luôn trang tiếp theo
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var jobNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'job-item-search-result')]");
            if (jobNodes == null || jobNodes.Count == 0)
            {
                _logger.LogWarning("TopCV: Không tìm thấy '.job-item-search-result' trên trang {PageNum}. Có thể ZenRows bypass thất bại hoặc HTML thay đổi.", pageNum);
                break; // Nếu không tìm thấy node nào thì break
            }

            _logger.LogInformation("TopCV: Tìm thấy {Count} job nodes trên trang {PageNum}.", jobNodes.Count, pageNum);

            var jobsInPage = new List<ScrapedJob>();

            foreach (var node in jobNodes)
            {
                var idAttr = node.GetAttributeValue("data-job-id", string.Empty);
                if (string.IsNullOrEmpty(idAttr)) continue;

                if (existingExternalIds.Contains(idAttr))
                {
                    continue; // Bỏ qua job cũ
                }

                var titleNode = node.SelectSingleNode(".//h3[contains(@class, 'title')]//a//span[@title] | .//h3[contains(@class, 'title')]//a");
                var companyNode = node.SelectSingleNode(".//a[contains(@class, 'company')]//span[contains(@class, 'company-name')]");
                var salaryNode = node.SelectSingleNode(".//label[contains(@class, 'title-salary')] | .//label[contains(@class, 'salary')]//span");
                var locationNode = node.SelectSingleNode(".//label[contains(@class, 'address')]//span[contains(@class, 'city-text')]");
                var linkNode = node.SelectSingleNode(".//h3[contains(@class, 'title')]//a");

                var title = titleNode?.InnerText?.Trim();
                var company = companyNode?.InnerText?.Trim();
                var salary = salaryNode?.InnerText?.Trim() ?? "Thoả thuận";
                var location = locationNode?.InnerText?.Trim() ?? "Không xác định";
                var href = linkNode?.GetAttributeValue("href", string.Empty)?.Trim();

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(href)) continue;

                jobsFound++;
                jobsInPage.Add(new ScrapedJob
                {
                    ExternalId = idAttr,
                    Title = HtmlEntity.DeEntitize(title),
                    CompanyName = HtmlEntity.DeEntitize(company),
                    Location = HtmlEntity.DeEntitize(location),
                    SalaryText = HtmlEntity.DeEntitize(salary),
                    SourceUrl = HtmlEntity.DeEntitize(href),
                    PostedAt = DateTime.UtcNow,
                    Description = HtmlEntity.DeEntitize(title) // TopCV search page ko có mô tả dài, tạm dùng title
                });

                // Nếu đã gom đủ số lượng job mới (vd 50) thì ngưng cào thêm trang này
                if (jobsFound >= _options.MaxJobsPerRun)
                {
                    _logger.LogInformation("TopCV: Đã gom đủ {JobsFound} job mới. Dừng trích xuất.", jobsFound);
                    break;
                }
            }

            foreach (var job in jobsInPage)
            {
                yield return job;
            }

            // Dừng cào trang tiếp theo nếu đã đủ job mới
            if (jobsFound >= _options.MaxJobsPerRun)
            {
                break;
            }

            // Mặc định gọi nhiều trang thì nghỉ 1s cho an toàn
            if (pageNum < _options.MaxPages)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        _logger.LogInformation("TopCV cào hoàn tất. Lấy được {JobsFound} jobs.", jobsFound);
    }
}
