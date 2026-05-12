using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class GoogleAuthService(
    AppDbContext dbContext,
    HttpClient httpClient,
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

        var payload = await VerifyGoogleTokenAsync(idToken, cancellationToken);

        if (!IsEmailVerified(payload.EmailVerified))
        {
            throw new UnauthorizedAccessException("Google email is not verified.");
        }

        if (!string.Equals(payload.Audience, _googleOptions.ClientId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Google token audience is invalid.");
        }

        if (!long.TryParse(payload.ExpiresAtUnixSeconds, out var expiresAtUnixSeconds) ||
            DateTimeOffset.FromUnixTimeSeconds(expiresAtUnixSeconds) <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Google token is expired.");
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
            new AuthUserResponse(user.Id, user.Email, user.FullName, user.AvatarUrl, user.Role));
    }

    private async Task<GoogleTokenInfoResponse> VerifyGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("Invalid Google token.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GoogleTokenInfoResponse>(
            cancellationToken: cancellationToken);

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new UnauthorizedAccessException("Invalid Google token payload.");
        }

        return payload;
    }

    private static bool IsEmailVerified(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var result) && result,
            _ => false
        };
    }

    private sealed record GoogleTokenInfoResponse(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_verified")] JsonElement EmailVerified,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("picture")] string? Picture,
        [property: JsonPropertyName("aud")] string Audience,
        [property: JsonPropertyName("exp")] string ExpiresAtUnixSeconds);
}
