namespace SWP_BE.Models;

public sealed class PortfolioProject
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }
    public Guid? GithubRepositoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TechStackJson { get; set; }
    public string? DemoUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
    public GithubRepository? GithubRepository { get; set; }
}
