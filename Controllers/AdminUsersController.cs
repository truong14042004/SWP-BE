using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        string? role,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim();
            query = query.Where(user => user.Role == normalizedRole);
        }

        if (isActive is not null)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => ToResponse(user))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> GetUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return user is null
            ? NotFound(new { message = "User was not found." })
            : Ok(ToResponse(user));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AdminUserResponse>> UpdateStatus(
        Guid id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (id == currentUserId)
        {
            return BadRequest(new { message = "Admin cannot delete the current account." });
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private static AdminUserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.IsEmailVerified,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
}

public sealed record UpdateUserStatusRequest(bool IsActive);

public sealed record AdminUserResponse(
    Guid Id,
    string? Username,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role,
    bool IsEmailVerified,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
