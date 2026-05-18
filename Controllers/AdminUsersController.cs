using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        UserRoles.Student,
        UserRoles.Admin,
        UserRoles.AcademicCounselor,
        UserRoles.IndustryMentor
    };

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

    [HttpPost]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var username = NormalizeRequired(request.Username!);
        var email = NormalizeRequired(request.Email!);
        var role = request.Role!.Trim();

        var duplicate = await dbContext.Users.AnyAsync(
            user => user.Username == username || user.Email == email,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Username or email already exists." });
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = request.FullName!.Trim(),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            Role = role,
            IsActive = request.IsActive ?? true,
            IsEmailVerified = request.IsEmailVerified ?? true,
            EmailVerifiedAt = request.IsEmailVerified is false ? null : now,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToResponse(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        Guid id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        var currentUserId = GetCurrentUserId();
        var role = request.Role!.Trim();
        if (id == currentUserId && (role != UserRoles.Admin || request.IsActive is false))
        {
            return BadRequest(new { message = "Admin cannot remove admin access or deactivate the current account." });
        }

        var username = NormalizeRequired(request.Username!);
        var email = NormalizeRequired(request.Email!);
        var duplicate = await dbContext.Users.AnyAsync(
            item => item.Id != id && (item.Username == username || item.Email == email),
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Username or email already exists." });
        }

        var shouldRevokeSessions = user.Role != role
            || !string.IsNullOrWhiteSpace(request.Password)
            || user.IsActive != (request.IsActive ?? user.IsActive);

        user.Username = username;
        user.Email = email;
        user.FullName = request.FullName!.Trim();
        user.AvatarUrl = NormalizeOptional(request.AvatarUrl);
        user.Role = role;
        user.IsActive = request.IsActive ?? user.IsActive;
        user.IsEmailVerified = request.IsEmailVerified ?? user.IsEmailVerified;
        user.EmailVerifiedAt = user.IsEmailVerified
            ? user.EmailVerifiedAt ?? DateTimeOffset.UtcNow
            : null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        if (shouldRevokeSessions)
        {
            await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
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

        if (id == GetCurrentUserId() && !request.IsActive)
        {
            return BadRequest(new { message = "Admin cannot deactivate the current account." });
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        if (!request.IsActive)
        {
            await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(user));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminUserResponse>> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Role) || !AllowedRoles.Contains(request.Role.Trim()))
        {
            return BadRequest(new { message = "User role is invalid." });
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        var role = request.Role.Trim();
        if (id == GetCurrentUserId() && role != UserRoles.Admin)
        {
            return BadRequest(new { message = "Admin cannot remove admin access from the current account." });
        }

        if (user.Role != role)
        {
            user.Role = role;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

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
        await RevokeUserRefreshTokensAsync(user.Id, cancellationToken);
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

    private async Task RevokeUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }

    private static string? ValidateCreateRequest(CreateAdminUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return "Username is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Password is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Role) || !AllowedRoles.Contains(request.Role.Trim()))
        {
            return "User role is invalid.";
        }

        return null;
    }

    private static string? ValidateUpdateRequest(UpdateAdminUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return "Username is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Role) || !AllowedRoles.Contains(request.Role.Trim()))
        {
            return "User role is invalid.";
        }

        return null;
    }

    private static string NormalizeRequired(string value) => value.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

public sealed record CreateAdminUserRequest(
    string? Username,
    string? Email,
    string? FullName,
    string? Password,
    string? Role,
    string? AvatarUrl,
    bool? IsEmailVerified,
    bool? IsActive);

public sealed record UpdateAdminUserRequest(
    string? Username,
    string? Email,
    string? FullName,
    string? Role,
    string? AvatarUrl,
    string? Password,
    bool? IsEmailVerified,
    bool? IsActive);

public sealed record UpdateUserStatusRequest(bool IsActive);

public sealed record UpdateUserRoleRequest(string? Role);

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
