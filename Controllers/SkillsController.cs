using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SkillsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSkills()
    {
        var skills = await _context.Skills
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Category,
                s.Description,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            })
            .ToListAsync();

        return Ok(skills);
    }
}
