namespace SWP_BE.Services;

public interface IMarketPulseRunner
{
    Task<MarketPulseRunResult> RunAsync(CancellationToken cancellationToken);
    Task<MarketPulseRunResult> ReExtractAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes stored jobs for the given source that no longer pass the IT
    /// classifier (title + description), along with their skill mentions.
    /// Used as a one-time cleanup of non-IT postings scraped before the filter
    /// existed. Returns the number of jobs removed.
    /// </summary>
    Task<int> PurgeNonItJobsAsync(string source, CancellationToken cancellationToken);
}

public sealed class MarketPulseRunResult
{
    public int JobsFetched { get; set; }
    public int JobsInserted { get; set; }
    public int JobsUpdated { get; set; }
    public int MentionsCreated { get; set; }
    public int SnapshotsCreated { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
}
