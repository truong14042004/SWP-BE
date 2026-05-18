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

        var now = DateTimeOffset.UtcNow;
        await dbContext.PendingRegistrations
            .Where(registration => registration.EmailVerificationOtpExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        var userExists = await dbContext.Users
            .AnyAsync(user => user.Username == username || user.Email == email, cancellationToken);
        if (userExists)
        {
            throw new InvalidOperationException("Username or email already exists.");
        }

        var otp = CreateOtp();
        var hashUser = new User { Username = username, Email = email, FullName = fullName };
        var pendingRegistration = await dbContext.PendingRegistrations
            .SingleOrDefaultAsync(
                registration => registration.Email == email || registration.Username == username,
                cancellationToken);

        if (pendingRegistration is not null
            && (pendingRegistration.Email != email || pendingRegistration.Username != username))
        {
            throw new InvalidOperationException("Username or email already exists.");
        }

        if (pendingRegistration is null)
        {
            pendingRegistration = new PendingRegistration
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            dbContext.PendingRegistrations.Add(pendingRegistration);
        }

        pendingRegistration.Username = username;
        pendingRegistration.Email = email;
        pendingRegistration.FullName = fullName;
        pendingRegistration.PasswordHash = passwordHasher.HashPassword(hashUser, request.Password);
        pendingRegistration.EmailVerificationOtpHash = passwordHasher.HashPassword(hashUser, otp);
        pendingRegistration.EmailVerificationOtpExpiresAt = now.Add(OtpLifetime);
        pendingRegistration.UpdatedAt = now;

        await emailSender.SendOtpAsync(pendingRegistration.Email, pendingRegistration.FullName, otp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthMessageResponse(
            "Verification OTP has been sent to your email.",
            pendingRegistration.Email);
    }

    public async Task<AuthResponse> VerifyEmailOtpAsync(
        VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);
        var pendingRegistration = await dbContext.PendingRegistrations
            .SingleOrDefaultAsync(registration => registration.Email == email, cancellationToken);

        if (pendingRegistration is null
            || pendingRegistration.EmailVerificationOtpExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        var hashUser = new User
        {
            Username = pendingRegistration.Username,
            Email = pendingRegistration.Email,
            FullName = pendingRegistration.FullName
        };
        var result = passwordHasher.VerifyHashedPassword(
            hashUser,
            pendingRegistration.EmailVerificationOtpHash,
            request.Otp);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        var userExists = await dbContext.Users.AnyAsync(
            user => user.Username == pendingRegistration.Username || user.Email == pendingRegistration.Email,
            cancellationToken);
        if (userExists)
        {
            throw new UnauthorizedAccessException("Username or email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = pendingRegistration.Username,
            Email = pendingRegistration.Email,
            FullName = pendingRegistration.FullName,
            PasswordHash = pendingRegistration.PasswordHash,
            Role = UserRoles.Student,
            IsActive = true,
            IsEmailVerified = true,
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);
        dbContext.PendingRegistrations.Remove(pendingRegistration);
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
