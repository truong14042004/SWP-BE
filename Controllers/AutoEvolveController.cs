using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/auto-evolve")]
public sealed class AutoEvolveController(
    AppDbContext dbContext,
    IAutoEvolveAiService autoEvolveAiService,
    ILogger<AutoEvolveController> logger) : ControllerBase
{
    // Kích hoạt AI phân tích thủ công
    [HttpPost("generate/{careerRoleId:guid}")]
    public async Task<IActionResult> GenerateProposals(Guid careerRoleId, CancellationToken cancellationToken)
    {
        try
        {
            await autoEvolveAiService.GenerateProposalsAsync(careerRoleId, cancellationToken);
            return Ok(new { message = "Đã sinh đề xuất thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi sinh đề xuất Auto-Evolve.");
            return StatusCode(500, new { message = "Lỗi hệ thống khi sinh đề xuất bằng AI." });
        }
    }

    // Lấy danh sách các đề xuất đang chờ duyệt
    [HttpGet("proposals")]
    public async Task<IActionResult> GetPendingProposals(CancellationToken cancellationToken)
    {
        var proposals = await dbContext.RoleSkillUpdateProposals
            .AsNoTracking()
            .Include(p => p.CareerRole)
            .Where(p => p.Status == "Pending")
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                CareerRoleId = p.CareerRoleId,
                CareerRoleName = p.CareerRole.Name,
                SkillId = p.SkillId,
                SkillName = p.SkillName,
                ActionType = p.ActionType,
                CurrentPriority = p.CurrentPriority,
                ProposedPriority = p.ProposedPriority,
                CurrentWeight = p.CurrentWeight,
                ProposedWeight = p.ProposedWeight,
                Reason = p.Reason,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(proposals);
    }

    // Duyệt (Approve) 1 đề xuất
    [HttpPost("proposals/{id:guid}/approve")]
    public async Task<IActionResult> ApproveProposal(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await dbContext.RoleSkillUpdateProposals
            .Include(p => p.CareerRole)
            .SingleOrDefaultAsync(p => p.Id == id && p.Status == "Pending", cancellationToken);

        if (proposal is null) return NotFound(new { message = "Không tìm thấy đề xuất chờ duyệt." });

        if (proposal.ActionType == "UpdatePriority" || proposal.ActionType == "UpdateWeight")
        {
            var req = await dbContext.RoleSkillRequirements
                .SingleOrDefaultAsync(r => r.CareerRoleId == proposal.CareerRoleId && r.SkillId == proposal.SkillId, cancellationToken);

            if (req is null) return BadRequest(new { message = "Không tìm thấy yêu cầu kỹ năng tương ứng để cập nhật." });

            if (proposal.ActionType == "UpdatePriority" && proposal.ProposedPriority.HasValue)
            {
                req.Priority = proposal.ProposedPriority.Value;
            }
            else if (proposal.ActionType == "UpdateWeight" && proposal.ProposedWeight.HasValue)
            {
                req.Weight = proposal.ProposedWeight.Value;
            }
            
            req.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (proposal.ActionType == "AddSkill")
        {
            var exists = await dbContext.RoleSkillRequirements
                .AnyAsync(r => r.CareerRoleId == proposal.CareerRoleId && r.SkillId == proposal.SkillId, cancellationToken);

            if (!exists)
            {
                dbContext.RoleSkillRequirements.Add(new RoleSkillRequirement
                {
                    Id = Guid.NewGuid(),
                    CareerRoleId = proposal.CareerRoleId,
                    SkillId = proposal.SkillId,
                    RequiredLevel = "Intermediate", // Mặc định
                    Priority = proposal.ProposedPriority ?? 3,
                    Weight = proposal.ProposedWeight ?? 1m,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        proposal.Status = "Approved";
        proposal.RespondedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã duyệt đề xuất." });
    }

    // Từ chối (Reject) 1 đề xuất
    [HttpPost("proposals/{id:guid}/reject")]
    public async Task<IActionResult> RejectProposal(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await dbContext.RoleSkillUpdateProposals
            .SingleOrDefaultAsync(p => p.Id == id && p.Status == "Pending", cancellationToken);

        if (proposal is null) return NotFound(new { message = "Không tìm thấy đề xuất chờ duyệt." });

        proposal.Status = "Rejected";
        proposal.RespondedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Đã từ chối đề xuất." });
    }
}
