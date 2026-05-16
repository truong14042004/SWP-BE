namespace SWP_BE.Models;

public sealed class MentorFeedback
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? PortfolioId { get; set; }
    public Guid? GithubRepositoryId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Mentor { get; set; } = null!;
    public User Student { get; set; } = null!;
    public Portfolio? Portfolio { get; set; }
    public GithubRepository? GithubRepository { get; set; }
}
