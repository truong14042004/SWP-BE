using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

/// <summary>
/// Cào việc làm TopCV bằng cách gọi sang microservice Python (Scrapling) đặt
/// riêng (xem thư mục SWP-Scraper). Backend C# chỉ điều phối + lưu DB; phần cào
/// nặng do service Python đảm nhiệm. Thay thế cho <see cref="TopCVScraper"/> cũ
/// vốn phụ thuộc ZenRows API trả phí.
/// </summary>
public sealed class ScraplingApiScraper : IJobScraper
{
    public string SourceName => "TopCV";

    private readonly HttpClient _httpClient;
    private readonly ScraplingApiOptions _options;
    private readonly string _internalToken;
    private readonly ILogger<ScraplingApiScraper> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ScraplingApiScraper(
        HttpClient httpClient,
        IOptions<MarketPulseOptions> options,
        IOptions<InternalAuthOptions> internalAuthOptions,
        ILogger<ScraplingApiScraper> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.ScraplingApi;
        _internalToken = internalAuthOptions.Value.Token;
        _logger = logger;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ScraplingApiScraper bị tắt trong cấu hình.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogWarning("ScraplingApi:BaseUrl chưa cấu hình. Bỏ qua cào TopCV.");
            yield break;
        }

        // Toàn bộ try/catch nằm trong helper vì C# không cho yield bên trong
        // khối try-catch.
        var jobs = await FetchJobsAsync(cancellationToken);

        var yielded = 0;
        foreach (var job in jobs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(job.ExternalId)
                || existingExternalIds.Contains(job.ExternalId))
            {
                continue;
            }

            yielded++;
            yield return job;
        }

        _logger.LogInformation(
            "ScraplingApiScraper: thu được {Count} job TopCV mới từ service Python.", yielded);
    }

    private async Task<List<ScrapedJob>> FetchJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/scrape/topcv"
                + $"?max_jobs={_options.MaxJobsPerRun}&max_pages={_options.MaxPages}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Ưu tiên token riêng của ScraplingApi nếu được set; mặc định dùng chung
            // InternalAuth:Token (đã có sẵn env trên Cloud Run) để khỏi quản lý 2 token.
            var token = !string.IsNullOrWhiteSpace(_options.Token)
                ? _options.Token
                : _internalToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.TryAddWithoutValidation("X-Internal-Token", token);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));

            _logger.LogInformation("ScraplingApi: gọi {Url} để cào TopCV.", url);

            using var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ScraplingApi trả status {Status} khi cào TopCV.", (int)response.StatusCode);
                return new List<ScrapedJob>();
            }

            var payload = await response.Content
                .ReadFromJsonAsync<ScrapeResponse>(JsonOptions, cts.Token);

            if (payload?.Jobs is null || payload.Jobs.Count == 0)
            {
                _logger.LogWarning("ScraplingApi không trả job nào cho TopCV.");
                return new List<ScrapedJob>();
            }

            return payload.Jobs
                .Select(MapToScrapedJob)
                .Where(job => !string.IsNullOrWhiteSpace(job.ExternalId)
                    && !string.IsNullOrWhiteSpace(job.Title))
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Lỗi gọi ScraplingApi để cào TopCV.");
            return new List<ScrapedJob>();
        }
    }

    private static ScrapedJob MapToScrapedJob(ScrapedJobDto dto) => new()
    {
        ExternalId = dto.ExternalId ?? string.Empty,
        Title = dto.Title ?? string.Empty,
        CompanyName = dto.CompanyName,
        Location = dto.Location,
        SalaryText = dto.SalaryText,
        SalaryMinMillionVnd = dto.SalaryMinMillionVnd,
        SalaryMaxMillionVnd = dto.SalaryMaxMillionVnd,
        Description = dto.Description,
        SourceUrl = dto.SourceUrl ?? string.Empty,
        PostedAt = dto.PostedAt,
    };

    private sealed class ScrapeResponse
    {
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("count")] public int Count { get; set; }
        [JsonPropertyName("jobs")] public List<ScrapedJobDto>? Jobs { get; set; }
    }

    private sealed class ScrapedJobDto
    {
        [JsonPropertyName("externalId")] public string? ExternalId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("salaryText")] public string? SalaryText { get; set; }
        [JsonPropertyName("salaryMinMillionVnd")] public decimal? SalaryMinMillionVnd { get; set; }
        [JsonPropertyName("salaryMaxMillionVnd")] public decimal? SalaryMaxMillionVnd { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("sourceUrl")] public string? SourceUrl { get; set; }
        [JsonPropertyName("postedAt")] public DateTimeOffset? PostedAt { get; set; }
    }
}
