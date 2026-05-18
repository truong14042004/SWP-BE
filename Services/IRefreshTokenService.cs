using SWP_BE.Contracts.Auth;
using SWP_BE.Models;

namespace SWP_BE.Services;

public interface IRefreshTokenService
{
    Task<AuthResponse> CreateSessionAsync(User user, CancellationToken cancellationToken);

    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
