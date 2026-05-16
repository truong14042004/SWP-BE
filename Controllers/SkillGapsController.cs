using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/skill-gap")]
public class SkillGapsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SkillGapsController(AppDbContext context)
    {
        _context = context;
    }

    public class AnalyzeSkillGapRequest
    {
        public Guid UserId { get; set; }
        public Guid? CareerRoleId { get; set; }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeSkillGap([FromBody] AnalyzeSkillGapRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
        }

        Guid targetRoleId;
        if (request.CareerRoleId.HasValue)
        {
            targetRoleId = request.CareerRoleId.Value;
        }
        else
        {
            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId);
            if (profile == null || !profile.TargetRoleId.HasValue)
            {
                return BadRequest(new { message = "Career role not provided and no target role found in student profile." });
            }
            targetRoleId = profile.TargetRoleId.Value;
        }

        var roleExists = await _context.CareerRoles.AnyAsync(r => r.Id == targetRoleId);
        if (!roleExists)
        {
            return NotFound(new { message = "Career role not found." });
        }

        // Get required skills for the role
        var requiredSkills = await _context.RoleSkillRequirements
            .Include(r => r.Skill)
            .Where(r => r.CareerRoleId == targetRoleId)
            .ToListAsync();

        if (!requiredSkills.Any())
        {
            return BadRequest(new { message = "This career role has no skill requirements." });
        }

        // Get user's current skills
        var userSkills = await _context.UserSkills
            .Where(u => u.UserId == request.UserId)
            .ToListAsync();

        var report = new SkillGapReport
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CareerRoleId = targetRoleId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Summary = ""
        };

        var reportItems = new List<SkillGapReportItem>();
        decimal totalScore = 0;
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
            
            item.CurrentLevel = userSkill?.Level ?? "None";

            if (userLevelValue >= reqLevelValue)
            {
                item.Status = "Mastered";
                item.Recommendation = "Great job! You have mastered this skill.";
                totalScore += req.Weight; // Full points
            }
            else if (userLevelValue > 0)
            {
                item.Status = "Needs Improvement";
                item.Recommendation = $"You need to improve from {userSkill!.Level} to {req.RequiredLevel}.";
                totalScore += req.Weight * (decimal)userLevelValue / reqLevelValue; // Partial points
            }
            else
            {
                item.Status = "Missing";
                item.Recommendation = $"You need to learn this skill up to {req.RequiredLevel} level.";
                // 0 points
            }

            totalWeight += req.Weight;
            reportItems.Add(item);
        }

        if (totalWeight > 0)
        {
            report.MatchScore = Math.Round((totalScore / totalWeight) * 100, 2);
        }
        else
        {
            report.MatchScore = 0;
        }

        report.Summary = $"You have a {report.MatchScore}% match for this career role.";

        // Delete previous reports for the same user and role
        var previousReports = await _context.SkillGapReports
            .Where(r => r.UserId == request.UserId && r.CareerRoleId == targetRoleId)
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
            report.MatchScore,
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
}
