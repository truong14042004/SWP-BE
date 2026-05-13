using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class PasswordAuthService(
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender) : IPasswordAuthService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

    public async Task<AuthMessageResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        var fullName = request.FullName.Trim();

        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Password confirmation does not match.");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Username and full name are required.");
        }

        var matchingUsers = await dbContext.Users
            .Where(user => user.Username == username || user.Email == email)
            .ToListAsync(cancellationToken);
        var existingUser = matchingUsers.Count == 1 ? matchingUsers[0] : null;

        if (matchingUsers.Count > 1
            || existingUser is not null
            && (existingUser.IsEmailVerified || existingUser.IsActive
                || existingUser.Username != username || existingUser.Email != email))
        {
            throw new InvalidOperationException("Username or email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = existingUser ?? new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
        };

        user.Username = username;
        user.Email = email;
        user.FullName = fullName;
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        user.IsActive = false;
        user.IsEmailVerified = false;
        user.EmailVerifiedAt = null;
        user.UpdatedAt = now;

        var otp = CreateOtp();
        user.EmailVerificationOtpHash = passwordHasher.HashPassword(user, otp);
        user.EmailVerificationOtpExpiresAt = now.Add(OtpLifetime);

        if (existingUser is null)
        {
            dbContext.Users.Add(user);
        }

        await emailSender.SendOtpAsync(user.Email, user.FullName, otp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthMessageResponse(
            "Verification OTP has been sent to your email.",
            user.Email);
    }

    public async Task<AuthResponse> VerifyEmailOtpAsync(
        VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Email == email, cancellationToken);

        if (user is null
            || user.IsEmailVerified
            || string.IsNullOrWhiteSpace(user.EmailVerificationOtpHash)
            || user.EmailVerificationOtpExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        var result = passwordHasher.VerifyHashedPassword(
            user,
            user.EmailVerificationOtpHash,
            request.Otp);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        user.IsActive = true;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTimeOffset.UtcNow;
        user.EmailVerificationOtpHash = null;
        user.EmailVerificationOtpExpiresAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

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

        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !user.IsActive
            || !user.IsEmailVerified)
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

    private static string CreateOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
