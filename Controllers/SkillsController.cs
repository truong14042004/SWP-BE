using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/skills")]
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
        var query = _context.Skills.AsQueryable();

        if (!User.IsInRole(UserRoles.IndustryMentor))
        {
            query = query.Where(s => s.IsActive);
        }

        var skills = await query
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
