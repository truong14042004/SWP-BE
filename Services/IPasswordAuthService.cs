using SWP_BE.Contracts.Auth;

namespace SWP_BE.Services;

public interface IPasswordAuthService
{
    Task<AuthMessageResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyEmailOtpAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(PasswordLoginRequest request, CancellationToken cancellationToken);
}
