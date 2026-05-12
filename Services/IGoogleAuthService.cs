using SWP_BE.Contracts.Auth;

namespace SWP_BE.Services;

public interface IGoogleAuthService
{
    Task<AuthResponse> LoginAsync(string idToken, CancellationToken cancellationToken);
}
