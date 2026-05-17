namespace SWP_BE.Contracts.Auth;

public sealed record MeResponse(
    Guid Id,
    string? Username,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Role,
    bool IsEmailVerified,
    bool IsActive);
