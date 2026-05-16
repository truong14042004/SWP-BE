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

        if (!User.IsInRole("Admin"))
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
            return NotFound(new { message = "Career role not found." });
        }

        return Ok(role);
    }

    public class CreateCareerRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Level { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCareerRole([FromBody] CreateCareerRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        var exists = await _context.CareerRoles.AnyAsync(r => r.Name == request.Name);
        if (exists)
        {
            return Conflict(new { message = "A career role with this name already exists." });
        }

        var role = new CareerRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Level = request.Level,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CareerRoles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCareerRole), new { id = role.Id }, new
        {
            role.Id,
            role.Name,
            role.Description,
            role.Level,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt
        });
    }

    public class UpdateCareerRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Level { get; set; }
        public bool IsActive { get; set; }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCareerRole(Guid id, [FromBody] UpdateCareerRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        var role = await _context.CareerRoles.FindAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Career role not found." });
        }

        var exists = await _context.CareerRoles.AnyAsync(r => r.Name == request.Name && r.Id != id);
        if (exists)
        {
            return Conflict(new { message = "Another career role with this name already exists." });
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.Level = request.Level;
        role.IsActive = request.IsActive;
        role.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            role.Id,
            role.Name,
            role.Description,
            role.Level,
            role.IsActive,
            role.CreatedAt,
            role.UpdatedAt
        });
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
            return Unauthorized(new { message = "Invalid token." });
        }

        var roleExists = await _context.CareerRoles.AnyAsync(r => r.Id == request.CareerRoleId);
        if (!roleExists)
        {
            return NotFound(new { message = "Career role not found." });
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
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
            message = "Career role selected successfully.", 
            profileId = profile.Id, 
            targetRoleId = profile.TargetRoleId 
        });
    }
}
