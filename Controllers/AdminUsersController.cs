using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IFileStorageService storageService) : ControllerBase
{
    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

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

    [HttpPost("{id:guid}/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdminUserResponse>> UploadAvatar(
        Guid id,
        [FromForm] UploadAdminUserAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User was not found." });
        }

        var validationError = ValidateAvatarFile(request.File);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var oldAvatarObjectName = IsUserObject(id, user.AvatarUrl) ? user.AvatarUrl : null;
        var objectName = BuildUserObjectName(id, "avatar", request.File!.FileName, request.File.ContentType);
        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        user.AvatarUrl = result.ObjectName;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(oldAvatarObjectName))
        {
            await storageService.DeleteAsync(oldAvatarObjectName, cancellationToken);
        }

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

    private static string? ValidateAvatarFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Avatar image is required.";
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return "Avatar image must be 5 MB or smaller.";
        }

        if (!ImageContentTypes.Contains(file.ContentType))
        {
            return $"Unsupported avatar content type: {file.ContentType}.";
        }

        return null;
    }

    private static string NormalizeRequired(string value) => value.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUserObject(Guid userId, string? objectName) =>
        !string.IsNullOrWhiteSpace(objectName)
        && !objectName.Contains("..", StringComparison.Ordinal)
        && objectName.StartsWith($"users/{userId}/", StringComparison.Ordinal);

    private static string BuildUserObjectName(
        Guid userId,
        string category,
        string? originalFileName,
        string? contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GetExtension(contentType);
        }

        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        baseName = string.IsNullOrWhiteSpace(baseName)
            ? "avatar"
            : new string(baseName
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "avatar";
        }

        extension = !string.IsNullOrWhiteSpace(extension)
            && extension.Length <= 11
            && extension[0] == '.'
            && extension.Skip(1).All(char.IsLetterOrDigit)
                ? extension.ToLowerInvariant()
                : string.Empty;

        return $"users/{userId}/{category}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{baseName}{extension}";
    }

    private static string GetExtension(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => string.Empty
        };
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

public sealed class UploadAdminUserAvatarRequest
{
    public IFormFile File { get; set; } = null!;
}

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
