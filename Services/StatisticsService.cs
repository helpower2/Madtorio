using Microsoft.EntityFrameworkCore;
using Madtorio.Data;
using Madtorio.Data.Models;

namespace Madtorio.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(ApplicationDbContext context, ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Download tracking
    public async Task LogDownloadAsync(int saveFileId, string? userId)
    {
        try
        {
            var downloadLog = new DownloadLog
            {
                SaveFileId = saveFileId,
                UserId = userId,
                DownloadDate = DateTime.UtcNow
            };

            _context.DownloadLogs.Add(downloadLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Download logged for SaveFileId: {SaveFileId}, UserId: {UserId}",
                saveFileId, userId ?? "Anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging download for SaveFileId: {SaveFileId}", saveFileId);
        }
    }

    public async Task<int> GetDownloadCountAsync(int saveFileId)
    {
        try
        {
            return await _context.DownloadLogs
                .Where(dl => dl.SaveFileId == saveFileId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download count for SaveFileId: {SaveFileId}", saveFileId);
            return 0;
        }
    }

    public async Task<Dictionary<int, int>> GetDownloadCountsAsync()
    {
        try
        {
            return await _context.DownloadLogs
                .GroupBy(dl => dl.SaveFileId)
                .Select(g => new { SaveFileId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SaveFileId, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download counts");
            return new Dictionary<int, int>();
        }
    }

    public async Task<List<DownloadLog>> GetRecentDownloadsAsync(int count = 10)
    {
        try
        {
            return await _context.DownloadLogs
                .Include(dl => dl.SaveFile)
                .OrderByDescending(dl => dl.DownloadDate)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent downloads");
            return new List<DownloadLog>();
        }
    }

    // Page view tracking
    public async Task LogPageViewAsync(string pagePath, string? userId)
    {
        try
        {
            var pageView = new PageView
            {
                PagePath = pagePath,
                UserId = userId,
                ViewDate = DateTime.UtcNow
            };

            _context.PageViews.Add(pageView);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Page view logged for {PagePath}, UserId: {UserId}",
                pagePath, userId ?? "Anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging page view for {PagePath}", pagePath);
        }
    }

    public async Task<int> GetPageViewCountAsync(string pagePath)
    {
        try
        {
            return await _context.PageViews
                .Where(pv => pv.PagePath == pagePath)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting page view count for {PagePath}", pagePath);
            return 0;
        }
    }

    public async Task<Dictionary<string, int>> GetAllPageViewCountsAsync()
    {
        try
        {
            return await _context.PageViews
                .GroupBy(pv => pv.PagePath)
                .Select(g => new { PagePath = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PagePath, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all page view counts");
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<PageView>> GetRecentPageViewsAsync(int count = 10)
    {
        try
        {
            return await _context.PageViews
                .OrderByDescending(pv => pv.ViewDate)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent page views");
            return new List<PageView>();
        }
    }

    // Dashboard statistics
    public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
    {
        try
        {
            var stats = new DashboardStatistics();

            // Get total saves and storage
            var saves = await _context.SaveFiles.ToListAsync();
            stats.TotalSaves = saves.Count;
            stats.TotalStorageBytes = saves.Sum(s => s.FileSize);

            // Get total downloads and page views
            stats.TotalDownloads = await _context.DownloadLogs.CountAsync();
            stats.TotalPageViews = await _context.PageViews.CountAsync();

            // Get top downloaded files (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            stats.TopDownloads = await _context.DownloadLogs
                .Where(dl => dl.DownloadDate >= thirtyDaysAgo)
                .GroupBy(dl => new { dl.SaveFileId, dl.SaveFile.FileName })
                .Select(g => new TopDownloadedFile
                {
                    SaveFileId = g.Key.SaveFileId,
                    FileName = g.Key.FileName,
                    DownloadCount = g.Count(),
                    LastDownloaded = g.Max(dl => dl.DownloadDate)
                })
                .OrderByDescending(t => t.DownloadCount)
                .Take(5)
                .ToListAsync();

            // Get recent downloads
            stats.RecentDownloads = await GetRecentDownloadsAsync(10);

            // Get page view breakdown
            stats.PageViewsByPath = await GetAllPageViewCountsAsync();

            // Get last 7 days activity
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
            var dailyStats = new List<DailyStatistic>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var nextDate = date.AddDays(1);

                var dailyStat = new DailyStatistic
                {
                    Date = date,
                    Downloads = await _context.DownloadLogs
                        .Where(dl => dl.DownloadDate >= date && dl.DownloadDate < nextDate)
                        .CountAsync(),
                    PageViews = await _context.PageViews
                        .Where(pv => pv.ViewDate >= date && pv.ViewDate < nextDate)
                        .CountAsync(),
                    Uploads = await _context.SaveFiles
                        .Where(sf => sf.UploadDate >= date && sf.UploadDate < nextDate)
                        .CountAsync()
                };

                dailyStats.Add(dailyStat);
            }

            stats.Last7Days = dailyStats;

            _logger.LogInformation("Dashboard statistics generated successfully");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard statistics");
            return new DashboardStatistics();
        }
    }
}
