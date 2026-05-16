namespace SWP_BE.Models;

public sealed class GithubOAuthState
{
    public string State { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
