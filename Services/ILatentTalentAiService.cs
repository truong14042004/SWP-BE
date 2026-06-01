using System.Threading;
using System.Threading.Tasks;

namespace SWP_BE.Services;

public class LatentTalentResultDto
{
    public int LogicalThinkingScore { get; set; }
    public int SystemArchitectureScore { get; set; }
    public int VisualDesignScore { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

public interface ILatentTalentAiService
{
    /// <summary>
    /// Phân tích danh sách Diff lấy từ Github để sinh ra điểm số tài năng và nhận xét.
    /// </summary>
    Task<LatentTalentResultDto> AnalyzeTalentFromCommitsAsync(string repoUrl, CancellationToken cancellationToken = default);
}
