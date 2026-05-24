namespace SWP_BE.Options;

public sealed class MarketPulseOptions
{
    public const string SectionName = "MarketPulse";

    public bool Enabled { get; set; } = true;
    public int ScheduleHour { get; set; } = 6;
    public int ScheduleMinute { get; set; } = 0;
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public int TrendWindowDays { get; set; } = 30;
    public int SnapshotRetentionDays { get; set; } = 180;
    public TopCvScraperOptions TopCv { get; set; } = new();
}

public sealed class TopCvScraperOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://www.topcv.vn";
    public string ListPath { get; set; } = "/viec-lam-it";
    public int MaxPages { get; set; } = 5;
    public int MaxJobsPerRun { get; set; } = 100;
    public int RequestDelayMs { get; set; } = 2500;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";
}
