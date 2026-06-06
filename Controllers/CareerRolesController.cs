using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/career-roles")]
public class CareerRolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CareerRolesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCareerRoles()
    {
        var query = _context.CareerRoles.AsQueryable();

        if (!User.IsInRole(UserRoles.IndustryMentor))
        {
            query = query.Where(r => r.IsActive);
        }

        var roles = await query
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Level,
                r.IsActive,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCareerRole(Guid id)
    {
        var role = await _context.CareerRoles
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Level,
                r.IsActive,
                r.CreatedAt,
                r.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (role == null)
        {
            return NotFound(new { message = "Không tìm thấy định hướng nghề nghiệp." });
        }

        return Ok(role);
    }

    public class SelectCareerRoleRequest
    {
        public Guid CareerRoleId { get; set; }
    }

    [HttpPost("select")]
    [Authorize]
    public async Task<IActionResult> SelectCareerRole([FromBody] SelectCareerRoleRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        var roleExists = await _context.CareerRoles.AnyAsync(r => r.Id == request.CareerRoleId);
        if (!roleExists)
        {
            return NotFound(new { message = "Không tìm thấy định hướng nghề nghiệp." });
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return NotFound(new { message = "Không tìm thấy người dùng." });
        }

        var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new StudentProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TargetRoleId = request.CareerRoleId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.StudentProfiles.Add(profile);
        }
        else
        {
            profile.TargetRoleId = request.CareerRoleId;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Chọn định hướng nghề nghiệp thành công.",
            profileId = profile.Id,
            targetRoleId = profile.TargetRoleId
        });
    }
}
