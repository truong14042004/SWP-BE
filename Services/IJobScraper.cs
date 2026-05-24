namespace SWP_BE.Services;

public interface IJobScraper
{
    string SourceName { get; }
    IAsyncEnumerable<ScrapedJob> ScrapeAsync(CancellationToken cancellationToken);
}

public sealed class ScrapedJob
{
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
}
