using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var roles = await _context.CareerRoles
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
}
