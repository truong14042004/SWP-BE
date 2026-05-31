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
    public VietnamWorksScraperOptions VietnamWorks { get; set; } = new();
    public TopCVScraperOptions TopCV { get; set; } = new();
}

public sealed class TopCVScraperOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257";
    public string ApiKey { get; set; } = "";
    public int MaxPages { get; set; } = 5; // Cho phép cào sâu hơn nếu chưa đủ job
    public int MaxJobsPerRun { get; set; } = 50; // Giới hạn số lượng job MỚI mỗi lần chạy
    public bool JsRender { get; set; } = true;
    public bool PremiumProxy { get; set; } = true;
}

public sealed class TopDevScraperOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://topdev.vn";
    public string SitemapIndexPath { get; set; } = "/sitemap-jobs.xml";
    public int MaxSitemapPages { get; set; } = 100; // Đào sâu
    public int MaxJobsPerRun { get; set; } = 60;
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
    public int MaxSitemapPages { get; set; } = 100; // Đào siêu sâu
    public int MaxJobsPerRun { get; set; } = 13;
    public int DelaySeconds { get; set; } = 1;
}

public sealed class VietnamWorksScraperOptions
{
    public bool Enabled { get; set; } = true;
    public string SitemapUrl { get; set; } = "https://www.vietnamworks.com/sitemap/jobs.xml";
    public int MaxSitemapJobs { get; set; } = 500; // Quét 500 job mới nhất trong sitemap
    public int MaxJobsPerRun { get; set; } = 14;
    public int DelaySeconds { get; set; } = 1;
}
