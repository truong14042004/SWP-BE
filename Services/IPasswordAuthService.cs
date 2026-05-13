using SWP_BE.Contracts.Auth;

namespace SWP_BE.Services;

public interface IPasswordAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(PasswordLoginRequest request, CancellationToken cancellationToken);
}
