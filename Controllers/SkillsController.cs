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

    public class CreateSkillRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { message = "Name and Category are required." });
        }

        // Kiểm tra xem skill đã tồn tại chưa (kết hợp Name và Category là unique trong DB)
        var exists = await _context.Skills.AnyAsync(s => s.Name == request.Name && s.Category == request.Category);
        if (exists)
        {
            return Conflict(new { message = "A skill with this name and category already exists." });
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            skill.Id,
            skill.Name,
            skill.Category,
            skill.Description,
            skill.IsActive,
            skill.CreatedAt,
            skill.UpdatedAt
        });
    }

    public class UpdateSkillRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] UpdateSkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { message = "Name and Category are required." });
        }

        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound(new { message = "Skill not found." });
        }

        var exists = await _context.Skills.AnyAsync(s => s.Name == request.Name && s.Category == request.Category && s.Id != id);
        if (exists)
        {
            return Conflict(new { message = "Another skill with this name and category already exists." });
        }

        skill.Name = request.Name;
        skill.Category = request.Category;
        skill.Description = request.Description;
        skill.IsActive = request.IsActive;
        skill.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            skill.Id,
            skill.Name,
            skill.Category,
            skill.Description,
            skill.IsActive,
            skill.CreatedAt,
            skill.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(Guid id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound(new { message = "Skill not found." });
        }

        try
        {
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Cannot delete this skill because it is being referenced by other records." });
        }
    }
}
