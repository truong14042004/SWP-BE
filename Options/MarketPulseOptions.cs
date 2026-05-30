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
    public TopDevScraperOptions TopDev { get; set; } = new();
    public ITNaviScraperOptions ITNavi { get; set; } = new();
}

public sealed class TopDevScraperOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://topdev.vn";
    public string SitemapIndexPath { get; set; } = "/sitemap-jobs.xml";
    public int MaxSitemapPages { get; set; } = 1;
    public int MaxJobsPerRun { get; set; } = 20;
    public int MinRequestDelayMs { get; set; } = 600;
    public int MaxRequestDelayMs { get; set; } = 1500;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public bool RefreshExistingJobs { get; set; } = false;
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";
}

public sealed class ITNaviScraperOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxSitemapPages { get; set; } = 20;
    public int MaxJobsPerRun { get; set; } = 20;
    public int DelaySeconds { get; set; } = 1;
}
