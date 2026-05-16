namespace SWP_BE.Models;

public sealed class Portfolio
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Theme { get; set; } = "Default";
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
