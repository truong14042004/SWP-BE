using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class PasswordAuthService(
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IPasswordHasher<User> passwordHasher) : IPasswordAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        var fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Username and full name are required.");
        }

        var alreadyExists = await dbContext.Users.AnyAsync(
            user => user.Username == username || user.Email == email,
            cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException("Username or email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = fullName,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        var username = Normalize(request.Username);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Username == username, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = jwtTokenService.CreateAccessToken(user);
        return new AuthResponse(
            token.Token,
            token.ExpiresAt,
            new AuthUserResponse(user.Id, user.Username, user.Email, user.FullName, user.AvatarUrl, user.Role));
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
