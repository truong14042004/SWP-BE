namespace SWP_BE.Models;

public sealed class JobPost
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? SalaryText { get; set; }
    public decimal? SalaryMinMillionVnd { get; set; }
    public decimal? SalaryMaxMillionVnd { get; set; }
    public string? Description { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public DateTimeOffset? PostedAt { get; set; }
    public DateTimeOffset ScrapedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
