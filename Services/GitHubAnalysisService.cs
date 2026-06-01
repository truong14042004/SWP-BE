using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SWP_BE.Services;

public class GitHubAnalysisService : IGitHubAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubAnalysisService> _logger;
    private readonly string _githubToken;

    public GitHubAnalysisService(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Thiết lập User-Agent bắt buộc bởi Github API
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CareerMap-App", "1.0"));
        
        // Lấy token từ appsettings.json
        _githubToken = configuration["GitHub:Token"] ?? string.Empty;
        if (!string.IsNullOrEmpty(_githubToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
        }
    }

    public async Task<List<GithubCommitInfo>> GetRecentCommitsWithDiffAsync(string repoUrl, int maxCommits = 10, CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ExtractOwnerAndRepo(repoUrl);
        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
        {
            throw new ArgumentException("Đường dẫn Github không hợp lệ. Vui lòng cung cấp link dạng https://github.com/owner/repo");
        }

        var commitsUrl = $"https://api.github.com/repos/{owner}/{repo}/commits?per_page={maxCommits}";
        _logger.LogInformation("Đang gọi Github API: {Url}", commitsUrl);

        var response = await _httpClient.GetAsync(commitsUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Lỗi khi gọi Github API: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Không thể truy cập Repository. Vui lòng đảm bảo Repo là Public hoặc Token có quyền truy cập. Lỗi: {response.StatusCode}");
        }

        var jsonStr = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonStr);

        var result = new List<GithubCommitInfo>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var sha = item.GetProperty("sha").GetString() ?? "";
            var commitNode = item.GetProperty("commit");
            var message = commitNode.GetProperty("message").GetString() ?? "";
            
            var authorNode = commitNode.GetProperty("author");
            var authorName = authorNode.GetProperty("name").GetString() ?? "";
            var date = authorNode.GetProperty("date").GetString() ?? "";

            // Lấy diff cho từng commit
            var diff = await GetCommitDiffAsync(owner, repo, sha, cancellationToken);

            result.Add(new GithubCommitInfo
            {
                Sha = sha,
                Message = message,
                AuthorName = authorName,
                Date = date,
                DiffContent = diff
            });
        }

        return result;
    }

    private async Task<string> GetCommitDiffAsync(string owner, string repo, string sha, CancellationToken cancellationToken)
    {
        var diffUrl = $"https://api.github.com/repos/{owner}/{repo}/commits/{sha}";
        
        // Tạo request đặc biệt yêu cầu Github trả về định dạng application/vnd.github.v3.diff
        var request = new HttpRequestMessage(HttpMethod.Get, diffUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var diffContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Giới hạn chiều dài diff để tránh quá tải Token Context của AI (Ví dụ: cắt 3000 ký tự đầu tiên)
            if (diffContent.Length > 3000)
            {
                return diffContent.Substring(0, 3000) + "\n...[DIFF QUÁ DÀI, ĐÃ BỊ CẮT BỚT]...";
            }
            return diffContent;
        }

        return "[KHÔNG THỂ LẤY DIFF]";
    }

    private (string? owner, string? repo) ExtractOwnerAndRepo(string url)
    {
        try
        {
            // Ví dụ: https://github.com/truong14042004/SWP-BE
            var match = Regex.Match(url, @"github\.com/([^/]+)/([^/]+)");
            if (match.Success)
            {
                var owner = match.Groups[1].Value;
                var repo = match.Groups[2].Value.Replace(".git", "");
                return (owner, repo);
            }
        }
        catch { }
        return (null, null);
    }
}
