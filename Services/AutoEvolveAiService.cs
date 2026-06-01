using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class AutoEvolveAiService(
    AppDbContext dbContext,
    IAiTextGenerationService aiService,
    ILogger<AutoEvolveAiService> logger) : IAutoEvolveAiService
{
    public async Task GenerateProposalsAsync(Guid careerRoleId, CancellationToken cancellationToken)
    {
        var role = await dbContext.CareerRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == careerRoleId && r.IsActive, cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException("Không tìm thấy định hướng nghề nghiệp hợp lệ.");
        }

        // 1. Lấy trạng thái hiện tại của RoleSkillRequirements
        var currentRequirements = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Include(r => r.Skill)
            .Where(r => r.CareerRoleId == careerRoleId)
            .Select(r => new { r.SkillId, r.Skill.Name, r.Priority, r.Weight })
            .ToListAsync(cancellationToken);

        // 2. Lấy độ hot của thị trường trong 30 ngày qua cho các skill thuộc Role này
        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var marketTrends = await dbContext.KeywordTrendSnapshots
            .AsNoTracking()
            .Include(t => t.Skill)
            .Where(t => t.SnapshotDate >= thirtyDaysAgo && t.SkillId != null)
            .GroupBy(t => t.SkillId)
            .Select(g => new
            {
                SkillId = g.Key,
                SkillName = g.First().Skill!.Name,
                TotalMentions = g.Sum(t => t.TotalMentions)
            })
            .ToListAsync(cancellationToken);

        // Chỉ lấy những skill có trong Role hoặc những skill có lượt đề cập cao (để gợi ý thêm)
        var relevantTrends = marketTrends
            .Where(t => currentRequirements.Any(r => r.SkillId == t.SkillId) || t.TotalMentions > 5)
            .OrderByDescending(t => t.TotalMentions)
            .ToList();

        // 3. Chuẩn bị Prompt cho Gemini
        var systemInstruction = @"Bạn là Chuyên gia thiết kế lộ trình học tập (Instructional Designer) và Chuyên viên phân tích thị trường công nghệ.
Nhiệm vụ của bạn là so sánh Yêu cầu kỹ năng hiện tại của một Định hướng nghề nghiệp với Dữ liệu nhu cầu thực tế của thị trường trong 30 ngày qua.
Bạn hãy đề xuất các thay đổi để cập nhật Lộ trình học (Roadmap) sao cho sát với thực tế nhất.
Cấu trúc dữ liệu: Priority (Độ ưu tiên từ 1-5, số NHỎ HƠN nghĩa là học TRƯỚC, quan trọng hơn), Weight (Trọng số 0.5-3.0, độ quan trọng khi tính toán).
- Nếu thị trường yêu cầu kỹ năng đó rất nhiều: Hãy đề xuất giảm Priority (để học sớm hơn) HOẶC tăng Weight.
- Nếu thị trường ít yêu cầu: Giữ nguyên hoặc tăng Priority (học sau), giảm Weight.
- Nếu có kỹ năng hot trên thị trường nhưng CHƯA có trong Yêu cầu hiện tại: Đề xuất AddSkill.

QUY TẮC NGHIÊM NGẶT:
- CHỈ đề xuất thay đổi nếu có sự chênh lệch lớn và cần thiết. Đừng thay đổi lặt vặt.
- TRẢ VỀ CHỈ MỘT MẢNG JSON HỢP LỆ chứa các đối tượng. Không bọc trong Markdown ```json...```. Không có text nào khác ngoài JSON.

Cấu trúc đối tượng JSON mong muốn:
{
    ""SkillId"": ""Guid (nếu có) hoặc null (nếu kỹ năng mới chưa biết Id)"",
    ""SkillName"": ""Tên kỹ năng"",
    ""ActionType"": ""UpdatePriority"" | ""UpdateWeight"" | ""AddSkill"",
    ""ProposedPriority"": int (1-5) hoặc null,
    ""ProposedWeight"": number (0.5-3.0) hoặc null,
    ""Reason"": ""Lý do ngắn gọn gọn giải thích tại sao cần thay đổi""
}";

        var currentReqJson = JsonSerializer.Serialize(currentRequirements);
        var marketTrendsJson = JsonSerializer.Serialize(relevantTrends);

        var userPrompt = $@"Định hướng nghề nghiệp: {role.Name}
        
Yêu cầu hiện tại:
{currentReqJson}

Dữ liệu thị trường 30 ngày qua (Tổng lượt đề cập trong tin tuyển dụng):
{marketTrendsJson}

Hãy sinh các Đề xuất cập nhật (JSON array) dựa trên phân tích của bạn.";

        // 4. Gọi Gemini
        logger.LogInformation("Calling Gemini to generate roadmap proposals for Role {RoleId}", careerRoleId);
        var aiResult = await aiService.GenerateAsync(systemInstruction, userPrompt, asJson: true, cancellationToken);

        // 5. Parse kết quả và lưu vào Database (Trạng thái Pending)
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var proposals = JsonSerializer.Deserialize<List<AiProposalDto>>(aiResult.Text, options);

            if (proposals is null || proposals.Count == 0)
            {
                logger.LogInformation("Gemini did not generate any proposals.");
                return;
            }

            var newProposals = new List<RoleSkillUpdateProposal>();
            foreach (var p in proposals)
            {
                // Validate
                var actionType = p.ActionType?.Trim() ?? "";
                if (actionType is not "UpdatePriority" and not "UpdateWeight" and not "AddSkill")
                    continue;

                Guid finalSkillId = Guid.Empty;
                if (p.SkillId.HasValue && p.SkillId != Guid.Empty)
                {
                    finalSkillId = p.SkillId.Value;
                }
                else
                {
                    // Nếu AI đề xuất thêm skill mới nhưng không trả về ID đúng, ta cố gắng map bằng Tên
                    var existingSkill = await dbContext.Skills.FirstOrDefaultAsync(s => s.Name.ToLower() == p.SkillName.ToLower(), cancellationToken);
                    if (existingSkill != null)
                        finalSkillId = existingSkill.Id;
                    else
                        continue; // Bỏ qua nếu skill thực sự không tồn tại trong DB (Chặn sinh rác)
                }

                // Tránh tạo trùng Proposal đang Pending
                var existingPending = await dbContext.RoleSkillUpdateProposals
                    .AnyAsync(prop => prop.CareerRoleId == careerRoleId && prop.SkillId == finalSkillId && prop.Status == "Pending", cancellationToken);
                
                if (existingPending) continue;

                var currentReq = currentRequirements.FirstOrDefault(r => r.SkillId == finalSkillId);

                newProposals.Add(new RoleSkillUpdateProposal
                {
                    Id = Guid.NewGuid(),
                    CareerRoleId = careerRoleId,
                    SkillId = finalSkillId,
                    SkillName = p.SkillName ?? "Unknown",
                    ActionType = actionType,
                    CurrentPriority = currentReq?.Priority,
                    ProposedPriority = p.ProposedPriority,
                    CurrentWeight = currentReq?.Weight,
                    ProposedWeight = p.ProposedWeight,
                    Reason = p.Reason ?? string.Empty,
                    Status = "Pending",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            if (newProposals.Count > 0)
            {
                dbContext.RoleSkillUpdateProposals.AddRange(newProposals);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Saved {Count} new proposals for Role {RoleId}", newProposals.Count, careerRoleId);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse JSON returned by Gemini. Raw output: {Output}", aiResult.Text);
            throw new InvalidOperationException("AI trả về định dạng không hợp lệ.");
        }
    }

    private sealed class AiProposalDto
    {
        public Guid? SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? ActionType { get; set; }
        public int? ProposedPriority { get; set; }
        public decimal? ProposedWeight { get; set; }
        public string? Reason { get; set; }
    }
}
