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
}
