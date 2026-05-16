namespace SWP_BE.Models;

public sealed class MentorSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? ContextJson { get; set; }
    public string? Model { get; set; }
    public int? TokensUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
