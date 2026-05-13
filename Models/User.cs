namespace SWP_BE.Models;

public sealed class User
{
    public Guid Id { get; set; }

    public string? Username { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? GoogleSubject { get; set; }

    public string? PasswordHash { get; set; }

    public bool IsEmailVerified { get; set; } = true;

    public string? EmailVerificationOtpHash { get; set; }

    public DateTimeOffset? EmailVerificationOtpExpiresAt { get; set; }

    public DateTimeOffset? EmailVerifiedAt { get; set; }

    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
