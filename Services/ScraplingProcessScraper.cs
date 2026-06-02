using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

/// <summary>
/// Cào việc làm TopCV bằng cách chạy script Python (Scrapling) như một tiến
/// trình con ngay trong cùng container, rồi đọc kết quả JSON qua stdout.
///
/// Khác với cách gọi HTTP (microservice tách rời), cách này gói cả Python vào
/// image .NET nên chỉ cần MỘT service Cloud Run duy nhất — không cần FastAPI,
/// không cổng mạng, không token liên-service. Backend C# vẫn giữ toàn quyền
/// điều phối + lưu DB; phần cào nặng do Python (Scrapling) đảm nhiệm.
/// </summary>
public sealed class ScraplingProcessScraper : IJobScraper
{
    public string SourceName => "TopCV";

    private readonly ScraplingOptions _options;
    private readonly TopCVScraperOptions _topCVOptions;
    private readonly ILogger<ScraplingProcessScraper> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ScraplingProcessScraper(
        IOptions<MarketPulseOptions> options,
        ILogger<ScraplingProcessScraper> logger)
    {
        _options = options.Value.Scrapling;
        _topCVOptions = options.Value.TopCV;
        _logger = logger;
    }

    public async IAsyncEnumerable<ScrapedJob> ScrapeAsync(
        IReadOnlySet<string> existingExternalIds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ScraplingProcessScraper bị tắt trong cấu hình.");
            yield break;
        }

        // try/catch nằm trong helper vì C# không cho yield bên trong try-catch.
        var jobs = await RunScraperAsync(cancellationToken);

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
            "ScraplingProcessScraper: thu được {Count} job TopCV mới từ script Python.", yielded);
    }

    private async Task<List<ScrapedJob>> RunScraperAsync(CancellationToken cancellationToken)
    {
        try
        {
            var scriptPath = ResolveScriptPath();
            if (!File.Exists(scriptPath))
            {
                _logger.LogWarning(
                    "Không tìm thấy script scraper tại {Path}. Bỏ qua cào TopCV.", scriptPath);
                return new List<ScrapedJob>();
            }

            var psi = new ProcessStartInfo
            {
                FileName = _options.PythonExecutable,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add("--json");
            psi.ArgumentList.Add("--max-jobs");
            psi.ArgumentList.Add(_options.MaxJobsPerRun.ToString());
            psi.ArgumentList.Add("--max-pages");
            psi.ArgumentList.Add(_options.MaxPages.ToString());
            
            if (!string.IsNullOrWhiteSpace(_topCVOptions.BaseUrl))
            {
                psi.ArgumentList.Add("--base-url");
                psi.ArgumentList.Add(_topCVOptions.BaseUrl);
            }

            using var process = new Process { StartInfo = psi };

            _logger.LogInformation(
                "Khởi chạy scraper Python: {Exe} {Script} --json --max-jobs {Jobs} --max-pages {Pages} --base-url {BaseUrl}",
                _options.PythonExecutable, scriptPath, _options.MaxJobsPerRun, _options.MaxPages, _topCVOptions.BaseUrl);

            process.Start();

            // Đọc song song stdout/stderr để tránh deadlock khi buffer đầy.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds)));

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                _logger.LogWarning(
                    "Scraper Python vượt quá {Timeout}s, đã hủy tiến trình.", _options.TimeoutSeconds);
                return new List<ScrapedJob>();
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.LogInformation("Scraper Python (stderr): {Stderr}", stderr.Trim());
            }

            if (process.ExitCode != 0)
            {
                _logger.LogWarning(
                    "Scraper Python thoát với mã {Code}. Bỏ qua cào TopCV.", process.ExitCode);
                return new List<ScrapedJob>();
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                _logger.LogWarning("Scraper Python không trả dữ liệu trên stdout.");
                return new List<ScrapedJob>();
            }

            var payload = JsonSerializer.Deserialize<ScrapeResponse>(stdout, JsonOptions);
            if (payload?.Jobs is null || payload.Jobs.Count == 0)
            {
                _logger.LogWarning("Scraper Python không có job nào cho TopCV.");
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
            _logger.LogError(exception, "Lỗi chạy scraper Python cho TopCV.");
            return new List<ScrapedJob>();
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Tiến trình có thể đã tự thoát; bỏ qua.
        }
    }

    /// <summary>
    /// Đường dẫn tới script Python. Nếu cấu hình là tuyệt đối thì dùng luôn
    /// (ví dụ /scraper/scraper_topcv.py trong container), ngược lại tính tương
    /// đối theo thư mục làm việc hiện tại (thư mục project khi chạy dotnet, hoặc
    /// /app trong container).
    /// </summary>
    private string ResolveScriptPath()
    {
        if (Path.IsPathRooted(_options.ScriptPath))
        {
            return _options.ScriptPath;
        }

        return Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), _options.ScriptPath));
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
