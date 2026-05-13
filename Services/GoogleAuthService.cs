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
    IJwtTokenService jwtTokenService,
    IOptions<GoogleAuthOptions> googleOptions) : IGoogleAuthService
{
    private readonly GoogleAuthOptions _googleOptions = googleOptions.Value;

    public async Task<AuthResponse> LoginAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleOptions.ClientId))
        {
            throw new InvalidOperationException("Google client id is not configured.");
        }

        var payload = await VerifyGoogleTokenAsync(idToken);

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email is not verified.");
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
            user.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.CreateAccessToken(user);
        return new AuthResponse(
            token.Token,
            token.ExpiresAt,
            new AuthUserResponse(user.Id, user.Username, user.Email, user.FullName, user.AvatarUrl, user.Role));
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
            throw new UnauthorizedAccessException("Invalid Google token.");
        }
    }
}
