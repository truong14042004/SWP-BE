namespace SWP_BE.Models;

public sealed class GithubConnection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long? GithubUserId { get; set; }
    public string GithubUsername { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public string? Scope { get; set; }
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
