using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class GoogleAuthService(
    AppDbContext dbContext,
    IRefreshTokenService refreshTokenService,
    IOptions<GoogleAuthOptions> googleOptions) : IGoogleAuthService
{
    private readonly GoogleAuthOptions _googleOptions = googleOptions.Value;

    public async Task<AuthResponse> LoginAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleOptions.ClientId))
        {
            throw new InvalidOperationException("Chưa cấu hình Google Client ID.");
        }

        var payload = await VerifyGoogleTokenAsync(idToken);

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException("Email Google chưa được xác thực.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Email == normalizedEmail, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = payload.Name ?? normalizedEmail,
                AvatarUrl = payload.Picture,
                GoogleSubject = payload.Subject,
                IsActive = true,
                IsEmailVerified = true,
                EmailVerifiedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.FullName = payload.Name ?? user.FullName;
            user.AvatarUrl = payload.Picture ?? user.AvatarUrl;
            user.GoogleSubject ??= payload.Subject;
            user.IsActive = true;
            user.IsEmailVerified = true;
            user.EmailVerifiedAt ??= now;
            user.EmailVerificationOtpHash = null;
            user.EmailVerificationOtpExpiresAt = null;
            user.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await refreshTokenService.CreateSessionAsync(user, cancellationToken);
    }

    private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_googleOptions.ClientId]
        };

        try
        {
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Mã xác thực Google không hợp lệ.");
        }
    }
}
