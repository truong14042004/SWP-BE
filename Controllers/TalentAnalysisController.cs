using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

public sealed record AnalyzeTalentRequest(string RepoUrl);

[ApiController]
[Authorize]
[Route("api/talent")]
public sealed class TalentAnalysisController(
    AppDbContext dbContext,
    ILatentTalentAiService latentTalentAiService,
    ILogger<TalentAnalysisController> logger) : ControllerBase
{
    // Kích hoạt AI phân tích Github Repo của học viên
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(AnalyzeTalentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RepoUrl))
        {
            return BadRequest(new { message = "Vui lòng cung cấp đường dẫn Github Repository." });
        }

        var userId = GetCurrentUserId();

        try
        {
            var result = await latentTalentAiService.AnalyzeTalentFromCommitsAsync(request.RepoUrl, cancellationToken);

            // Lưu hoặc cập nhật hồ sơ tài năng của học viên
            var profile = await dbContext.StudentTalentProfiles
                .SingleOrDefaultAsync(p => p.StudentId == userId, cancellationToken);

            if (profile is null)
            {
                profile = new StudentTalentProfile
                {
                    Id = Guid.NewGuid(),
                    StudentId = userId
                };
                dbContext.StudentTalentProfiles.Add(profile);
            }

            profile.AnalyzedRepoUrl = request.RepoUrl;
            profile.LogicalThinkingScore = result.LogicalThinkingScore;
            profile.SystemArchitectureScore = result.SystemArchitectureScore;
            profile.VisualDesignScore = result.VisualDesignScore;
            profile.AiFeedback = result.Feedback;
            profile.AnalyzedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                logicalThinkingScore = profile.LogicalThinkingScore,
                systemArchitectureScore = profile.SystemArchitectureScore,
                visualDesignScore = profile.VisualDesignScore,
                feedback = profile.AiFeedback,
                analyzedRepoUrl = profile.AnalyzedRepoUrl,
                analyzedAt = profile.AnalyzedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi phân tích tài năng từ Github.");
            return StatusCode(500, new { message = "Lỗi hệ thống khi phân tích bằng AI." });
        }
    }

    // Lấy hồ sơ tài năng hiện tại của học viên (để vẽ Radar Chart)
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var profile = await dbContext.StudentTalentProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.StudentId == userId, cancellationToken);

        if (profile is null)
        {
            return Ok(new { hasProfile = false });
        }

        return Ok(new
        {
            hasProfile = true,
            logicalThinkingScore = profile.LogicalThinkingScore,
            systemArchitectureScore = profile.SystemArchitectureScore,
            visualDesignScore = profile.VisualDesignScore,
            feedback = profile.AiFeedback,
            analyzedRepoUrl = profile.AnalyzedRepoUrl,
            analyzedAt = profile.AnalyzedAt
        });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }
}
