using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/skill-gap")]
[Authorize]
public class SkillGapsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SkillGapsController(AppDbContext context)
    {
        _context = context;
    }

    public class AnalyzeSkillGapRequest
    {
        public Guid? CareerRoleId { get; set; }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeSkillGap([FromBody] AnalyzeSkillGapRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Mã xác thực không hợp lệ." });
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        Guid targetRoleId;
        if (request.CareerRoleId.HasValue)
        {
            targetRoleId = request.CareerRoleId.Value;
        }
        else
        {
            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null || !profile.TargetRoleId.HasValue)
            {
                return BadRequest(new { message = "Vai trò nghề nghiệp không được cung cấp và không tìm thấy vai trò mục tiêu trong hồ sơ sinh viên." });
            }
            targetRoleId = profile.TargetRoleId.Value;
        }

        var roleExists = await _context.CareerRoles.AnyAsync(r => r.Id == targetRoleId);
        if (!roleExists)
        {
            return NotFound(new { message = "Không tìm thấy vai trò nghề nghiệp." });
        }

        // Get required skills for the role
        var requiredSkills = await _context.RoleSkillRequirements
            .Include(r => r.Skill)
            .Where(r => r.CareerRoleId == targetRoleId)
            .ToListAsync();

        if (!requiredSkills.Any())
        {
            return BadRequest(new { message = "Vai trò nghề nghiệp này không có yêu cầu kỹ năng nào." });
        }

        // Get user's current skills
        var userSkills = await _context.UserSkills
            .Where(u => u.UserId == userId)
            .ToListAsync();

        var report = new SkillGapReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CareerRoleId = targetRoleId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Summary = ""
        };

        var reportItems = new List<SkillGapReportItem>();
        decimal selfReportedScore = 0;
        decimal verifiedScore = 0;
        decimal totalWeight = 0;

        foreach (var req in requiredSkills)
        {
            var userSkill = userSkills.FirstOrDefault(u => u.SkillId == req.SkillId);
            var item = new SkillGapReportItem
            {
                Id = Guid.NewGuid(),
                SkillGapReportId = report.Id,
                SkillId = req.SkillId,
                RequiredLevel = req.RequiredLevel,
                Priority = req.Priority,
                CreatedAt = DateTimeOffset.UtcNow
            };

            int reqLevelValue = ParseLevel(req.RequiredLevel);
            int userLevelValue = userSkill != null ? ParseLevel(userSkill.Level) : 0;
            int verifiedLevelValue = userSkill is { IsVerified: true }
                ? ParseLevel(string.IsNullOrWhiteSpace(userSkill.VerifiedLevel) ? userSkill.Level : userSkill.VerifiedLevel)
                : 0;

            item.CurrentLevel = userSkill?.Level ?? "None";

            if (userSkill != null && userLevelValue >= reqLevelValue)
            {
                selfReportedScore += req.Weight;

                if (userSkill.IsVerified)
                {
                    item.Status = "Matched";
                    item.Recommendation = "Làm tốt lắm! Bạn đã thành thạo và xác thực kỹ năng này.";
                }
                else
                {
                    item.Status = "NotVerified";
                    item.Recommendation = "Bạn đã đạt cấp độ kỹ năng yêu cầu, nhưng cần phải được xác thực (cung cấp minh chứng).";
                }
            }
            else if (userSkill != null && userLevelValue > 0)
            {
                item.Status = "Weak";
                item.Recommendation = $"Bạn cần cải thiện từ cấp độ {userSkill.Level} lên {req.RequiredLevel}.";
                selfReportedScore += req.Weight * (decimal)userLevelValue / reqLevelValue;
            }
            else
            {
                item.Status = "Missing";
                item.Recommendation = $"Bạn cần học kỹ năng này đến cấp độ {req.RequiredLevel}.";
            }

            if (userSkill is { IsVerified: true } && verifiedLevelValue >= reqLevelValue)
            {
                verifiedScore += req.Weight;
            }

            totalWeight += req.Weight;
            reportItems.Add(item);
        }

        if (totalWeight > 0)
        {
            report.MatchScore = Math.Round((selfReportedScore / totalWeight) * 100, 2);
            report.VerifiedMatchScore = Math.Round((verifiedScore / totalWeight) * 100, 2);
        }
        else
        {
            report.MatchScore = 0;
            report.VerifiedMatchScore = 0;
        }

        report.Summary = $"Mức độ phù hợp đã xác thực của bạn với vai trò nghề nghiệp này là {report.VerifiedMatchScore}%. Mức độ tự đánh giá: {report.MatchScore}%.";

        // Delete previous reports for the same user and role
        var previousReports = await _context.SkillGapReports
            .Where(r => r.UserId == userId && r.CareerRoleId == targetRoleId)
            .ToListAsync();
            
        if (previousReports.Any())
        {
            _context.SkillGapReports.RemoveRange(previousReports);
            // Cascading delete might happen, but to be safe we can also delete items
            var previousReportIds = previousReports.Select(r => r.Id).ToList();
            var previousItems = await _context.SkillGapReportItems
                .Where(i => previousReportIds.Contains(i.SkillGapReportId))
                .ToListAsync();
            _context.SkillGapReportItems.RemoveRange(previousItems);
        }

        _context.SkillGapReports.Add(report);
        _context.SkillGapReportItems.AddRange(reportItems);
        
        await _context.SaveChangesAsync();

        return Ok(new
        {
            report.Id,
            report.UserId,
            report.CareerRoleId,
            SelfReportedMatchScore = report.MatchScore,
            report.MatchScore,
            report.VerifiedMatchScore,
            report.Summary,
            report.CreatedAt,
            Items = reportItems.Select(i => new
            {
                i.SkillId,
                SkillName = requiredSkills.First(r => r.SkillId == i.SkillId).Skill.Name,
                i.CurrentLevel,
                i.RequiredLevel,
                i.Status,
                i.Priority,
                i.Recommendation
            })
        });
    }

    private int ParseLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level)) return 0;
        
        return level.ToLower() switch
        {
            "none" => 0,
            "beginner" => 1,
            "intermediate" => 2,
            "advanced" => 3,
            "expert" => 4,
            _ => 1 // Default if unknown format
        };
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestSkillGapReport()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Mã xác thực không hợp lệ." });
        }

        var report = await _context.SkillGapReports
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (report == null)
        {
            return NotFound(new { message = "Không tìm thấy báo cáo khoảng cách kỹ năng nào cho người dùng này." });
        }

        var items = await _context.SkillGapReportItems
            .Include(i => i.Skill)
            .Where(i => i.SkillGapReportId == report.Id)
            .ToListAsync();

        return Ok(new
        {
            report.Id,
            report.UserId,
            report.CareerRoleId,
            CareerRoleName = report.CareerRole.Name,
            SelfReportedMatchScore = report.MatchScore,
            report.MatchScore,
            report.VerifiedMatchScore,
            report.Summary,
            report.CreatedAt,
            Items = items.Select(i => new
            {
                i.SkillId,
                SkillName = i.Skill.Name,
                i.CurrentLevel,
                i.RequiredLevel,
                i.Status,
                i.Priority,
                i.Recommendation
            })
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSkillGapReportById(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var currentUserId))
        {
            return Unauthorized(new { message = "Mã xác thực không hợp lệ." });
        }

        var report = await _context.SkillGapReports
            .Include(r => r.CareerRole)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
        {
            return NotFound(new { message = "Không tìm thấy báo cáo khoảng cách kỹ năng." });
        }

        if (report.UserId != currentUserId && !User.IsInRole(UserRoles.Admin) && !User.IsInRole(UserRoles.AcademicCounselor))
        {
            return Forbid();
        }

        var items = await _context.SkillGapReportItems
            .Include(i => i.Skill)
            .Where(i => i.SkillGapReportId == report.Id)
            .ToListAsync();

        return Ok(new
        {
            report.Id,
            report.UserId,
            report.CareerRoleId,
            CareerRoleName = report.CareerRole.Name,
            SelfReportedMatchScore = report.MatchScore,
            report.MatchScore,
            report.VerifiedMatchScore,
            report.Summary,
            report.CreatedAt,
            Items = items.Select(i => new
            {
                i.SkillId,
                SkillName = i.Skill.Name,
                i.CurrentLevel,
                i.RequiredLevel,
                i.Status,
                i.Priority,
                i.Recommendation
            })
        });
    }
}
