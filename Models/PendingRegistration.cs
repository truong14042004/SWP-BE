namespace SWP_BE.Models;

public sealed class PendingRegistration
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string EmailVerificationOtpHash { get; set; } = string.Empty;

    public DateTimeOffset EmailVerificationOtpExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
