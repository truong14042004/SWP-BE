using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SWP_BE.Services;

public class LatentTalentAiService : ILatentTalentAiService
{
    private readonly IGitHubAnalysisService _gitHubAnalysisService;
    private readonly IAiTextGenerationService _aiService;
    private readonly ILogger<LatentTalentAiService> _logger;

    public LatentTalentAiService(
        IGitHubAnalysisService gitHubAnalysisService, 
        IAiTextGenerationService aiService, 
        ILogger<LatentTalentAiService> logger)
    {
        _gitHubAnalysisService = gitHubAnalysisService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<LatentTalentResultDto> AnalyzeTalentFromCommitsAsync(string repoUrl, CancellationToken cancellationToken = default)
    {
        // 1. Kéo dữ liệu diff từ Github
        var commits = await _gitHubAnalysisService.GetRecentCommitsWithDiffAsync(repoUrl, maxCommits: 10, cancellationToken);
        if (commits.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy commit nào trong Repository này.");
        }

        // 2. Gom dữ liệu vào Prompt
        var diffSummary = string.Join("\n\n---\n", commits.Select(c => 
            $"Commit: {c.Sha}\nMessage: {c.Message}\nAuthor: {c.AuthorName}\nDate: {c.Date}\nDiff:\n{c.DiffContent}"));

        var systemInstruction = @"Bạn là một chuyên gia đánh giá năng lực lập trình.
Nhiệm vụ của bạn là đọc lịch sử thay đổi mã nguồn (Git Commits Diff) của học viên và đánh giá phong cách lập trình (Latent Talent) của họ dựa trên 3 tiêu chí cốt lõi.

Các tiêu chí đánh giá (Thang điểm 1-10):
1. 'LogicalThinkingScore': Dấu hiệu tối ưu thuật toán, phân tích logic rẽ nhánh phức tạp, tái sử dụng hàm (DRY), bắt lỗi tốt.
2. 'SystemArchitectureScore': Dấu hiệu chia nhỏ commit rõ ràng có hệ thống, cấu trúc file/module tốt, tuân thủ Clean Architecture hoặc design pattern.
3. 'VisualDesignScore': Dấu hiệu quan tâm đến giao diện (HTML/CSS), đặt tên class BEM, sử dụng biến CSS, quan tâm UX/Animation.

Hãy trả về CHÍNH XÁC một đối tượng JSON với cấu trúc sau:
{
  ""logicalThinkingScore"": [số nguyên 1-10],
  ""systemArchitectureScore"": [số nguyên 1-10],
  ""visualDesignScore"": [số nguyên 1-10],
  ""feedback"": ""[Đoạn văn 3-4 câu nhận xét về phong cách code của học viên, chỉ ra điểm mạnh nhất và gợi ý hướng phát triển]""
}";

        var userPrompt = $"Dưới đây là lịch sử 10 commit gần nhất từ repository của học viên.\n\n{diffSummary}\n\nHãy phân tích và trả về JSON.";

        _logger.LogInformation("Đang gửi lịch sử Git sang AI để phân tích...");

        // 3. Gọi AI model (Bật chế độ JSON mode)
        var aiResult = await _aiService.GenerateAsync(systemInstruction, userPrompt, asJson: true, cancellationToken);
        var aiResponse = aiResult.Text;

        // 4. Parse kết quả JSON
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultDto = JsonSerializer.Deserialize<LatentTalentResultDto>(aiResponse, options);
            if (resultDto == null) throw new InvalidOperationException("AI trả về kết quả rỗng.");
            
            // Validate điểm trong khoảng 1-10
            resultDto.LogicalThinkingScore = Math.Clamp(resultDto.LogicalThinkingScore, 1, 10);
            resultDto.SystemArchitectureScore = Math.Clamp(resultDto.SystemArchitectureScore, 1, 10);
            resultDto.VisualDesignScore = Math.Clamp(resultDto.VisualDesignScore, 1, 10);

            return resultDto;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Lỗi Parse JSON từ AI. Nội dung trả về: {Response}", aiResponse);
            throw new InvalidOperationException("Kết quả trả về từ AI không đúng định dạng JSON.", ex);
        }
    }
}
