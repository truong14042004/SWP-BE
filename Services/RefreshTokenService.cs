using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class RefreshTokenService(
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService) : IRefreshTokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<AuthResponse> CreateSessionAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = CreateRefreshToken();
        var now = DateTimeOffset.UtcNow;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = now.Add(RefreshTokenLifetime),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResponse(user, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Mã làm mới (refresh token) không hợp lệ.");
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null
            || storedToken.RevokedAt is not null
            || storedToken.ExpiresAt <= now
            || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Mã làm mới (refresh token) không hợp lệ hoặc đã hết hạn.");
        }

        var replacementToken = CreateRefreshToken();
        var replacementHash = HashToken(replacementToken);

        storedToken.RevokedAt = now;
        storedToken.ReplacedByTokenHash = replacementHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = replacementHash,
            ExpiresAt = now.Add(RefreshTokenLifetime),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateResponse(storedToken.User, replacementToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse CreateResponse(User user, string refreshToken)
    {
        var token = jwtTokenService.CreateAccessToken(user);
        return new AuthResponse(
            token.Token,
            refreshToken,
            token.ExpiresAt,
            new AuthUserResponse(user.Id, user.Username, user.Email, user.FullName, user.AvatarUrl, user.Role));
    }

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string refreshToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
