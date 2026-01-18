namespace Madtorio.Data.Models;

public class DashboardStatistics
{
    public int TotalSaves { get; set; }
    public long TotalStorageBytes { get; set; }
    public int TotalDownloads { get; set; }
    public int TotalPageViews { get; set; }

    // Top downloaded files (last 30 days)
    public List<TopDownloadedFile> TopDownloads { get; set; } = new();

    // Recent activity
    public List<DownloadLog> RecentDownloads { get; set; } = new();

    // Page view breakdown
    public Dictionary<string, int> PageViewsByPath { get; set; } = new();

    // Time-based stats (last 7 days)
    public List<DailyStatistic> Last7Days { get; set; } = new();
}

public class TopDownloadedFile
{
    public int SaveFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public DateTime? LastDownloaded { get; set; }
}

public class DailyStatistic
{
    public DateTime Date { get; set; }
    public int Downloads { get; set; }
    public int PageViews { get; set; }
    public int Uploads { get; set; }
}
