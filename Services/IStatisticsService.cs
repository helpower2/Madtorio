using Madtorio.Data.Models;

namespace Madtorio.Services;

public interface IStatisticsService
{
    // Download tracking (GDPR-compliant)
    Task LogDownloadAsync(int saveFileId, string? userId);
    Task<int> GetDownloadCountAsync(int saveFileId);
    Task<Dictionary<int, int>> GetDownloadCountsAsync();
    Task<List<DownloadLog>> GetRecentDownloadsAsync(int count = 10);

    // Page view tracking (GDPR-compliant)
    Task LogPageViewAsync(string pagePath, string? userId);
    Task<int> GetPageViewCountAsync(string pagePath);
    Task<Dictionary<string, int>> GetAllPageViewCountsAsync();
    Task<List<PageView>> GetRecentPageViewsAsync(int count = 10);

    // Dashboard statistics
    Task<DashboardStatistics> GetDashboardStatisticsAsync();
}
