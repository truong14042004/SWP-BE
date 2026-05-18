namespace SWP_BE.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthUserResponse User);

public sealed record AuthUserResponse(
    Guid Id,
    string? Username,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role);
