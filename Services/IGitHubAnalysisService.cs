using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SWP_BE.Services;

public class GithubCommitInfo
{
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DiffContent { get; set; } = string.Empty; // Mã nguồn thay đổi (diff)
}

public interface IGitHubAnalysisService
{
    /// <summary>
    /// Phân tích URL Github để lấy Owner và Repo, sau đó kéo danh sách các commit gần nhất kèm Diff.
    /// </summary>
    Task<List<GithubCommitInfo>> GetRecentCommitsWithDiffAsync(string repoUrl, int maxCommits = 10, CancellationToken cancellationToken = default);
}
