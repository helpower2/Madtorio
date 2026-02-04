using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Madtorio.Data;
using Madtorio.Data.Models;

namespace Madtorio.Services;

public partial class ModRequestService : IModRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModRequestService> _logger;
    private readonly IRulesService _rulesService;
    private const int DailyRequestLimit = 5;
    private const string VoteResetConfigKey = "ModRequests_LastVoteReset";

    public ModRequestService(
        ApplicationDbContext context,
        HttpClient httpClient,
        ILogger<ModRequestService> logger,
        IRulesService rulesService)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _rulesService = rulesService;
    }

    public async Task<List<ModRequest>> GetAllRequestsAsync(ModRequestStatus? statusFilter = null)
    {
        try
        {
            var query = _context.ModRequests
                .Include(m => m.Votes)
                .AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(m => m.Status == statusFilter.Value);
            }

            return await query
                .OrderByDescending(m => m.Votes.Count)
                .ThenByDescending(m => m.RequestCount)
                .ThenByDescending(m => m.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mod requests");
            return new List<ModRequest>();
        }
    }

    public async Task<ModRequest?> GetRequestByIdAsync(int id)
    {
        try
        {
            return await _context.ModRequests
                .Include(m => m.Votes)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mod request with ID: {Id}", id);
            return null;
        }
    }

    public async Task<ModRequest?> GetRequestByModNameAsync(string modName)
    {
        try
        {
            return await _context.ModRequests
                .Include(m => m.Votes)
                .FirstOrDefaultAsync(m => m.ModName == modName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mod request with name: {ModName}", modName);
            return null;
        }
    }

    public async Task<int> GetUserRequestCountTodayAsync(string username)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            return await _context.ModRequestLogs
                .CountAsync(l => l.Username == username && l.RequestedAt.Date == today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request count for user: {Username}", username);
            return 0;
        }
    }

    public async Task<bool> CanUserRequestTodayAsync(string username)
    {
        var count = await GetUserRequestCountTodayAsync(username);
        return count < DailyRequestLimit;
    }

    public async Task<(ModRequest Request, bool IsNew)> CreateOrIncrementRequestAsync(string modUrl, string requestedBy)
    {
        // Validate daily limit
        if (!await CanUserRequestTodayAsync(requestedBy))
        {
            throw new InvalidOperationException("Daily request limit reached");
        }

        // Extract mod name from URL
        var modName = ExtractModNameFromUrl(modUrl);
        if (string.IsNullOrEmpty(modName))
        {
            throw new ArgumentException("Invalid mod portal URL");
        }

        // Check if mod already exists
        var existingRequest = await GetRequestByModNameAsync(modName);

        if (existingRequest != null)
        {
            // Increment request count
            existingRequest.RequestCount++;
            existingRequest.ModifiedDate = DateTime.UtcNow;
            _context.ModRequests.Update(existingRequest);

            // Log this request
            _context.ModRequestLogs.Add(new ModRequestLog
            {
                Username = requestedBy,
                ModRequestId = existingRequest.Id,
                RequestedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Mod request incremented for {ModName} by {User}", modName, requestedBy);

            return (existingRequest, false);
        }

        // Fetch metadata from Factorio API
        var metadata = await FetchModMetadataAsync(modName);
        if (metadata == null)
        {
            throw new InvalidOperationException($"Could not fetch metadata for mod: {modName}");
        }

        // Create new request
        var newRequest = new ModRequest
        {
            ModName = metadata.Name,
            ModTitle = metadata.Title,
            ModUrl = modUrl,
            ThumbnailUrl = metadata.Thumbnail,
            Author = metadata.Owner,
            Summary = metadata.Summary,
            Category = metadata.Category,
            DownloadsCount = metadata.DownloadsCount,
            RequestedBy = requestedBy,
            RequestCount = 1,
            Status = ModRequestStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        _context.ModRequests.Add(newRequest);
        await _context.SaveChangesAsync();

        // Log this request
        _context.ModRequestLogs.Add(new ModRequestLog
        {
            Username = requestedBy,
            ModRequestId = newRequest.Id,
            RequestedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        _logger.LogInformation("New mod request created for {ModName} by {User}", modName, requestedBy);

        return (newRequest, true);
    }

    public async Task<bool> UpdateStatusAsync(int id, ModRequestStatus status)
    {
        try
        {
            var request = await _context.ModRequests.FindAsync(id);
            if (request == null)
            {
                _logger.LogWarning("Mod request not found for status update: {Id}", id);
                return false;
            }

            request.Status = status;
            request.ModifiedDate = DateTime.UtcNow;
            _context.ModRequests.Update(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mod request {Id} status updated to {Status}", id, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating mod request status: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteRequestAsync(int id)
    {
        try
        {
            var request = await _context.ModRequests.FindAsync(id);
            if (request == null)
            {
                _logger.LogWarning("Mod request not found for deletion: {Id}", id);
                return false;
            }

            _context.ModRequests.Remove(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Mod request deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mod request: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ToggleVoteAsync(int modRequestId, string username)
    {
        try
        {
            var existingVote = await _context.ModRequestVotes
                .FirstOrDefaultAsync(v => v.ModRequestId == modRequestId && v.Username == username);

            if (existingVote != null)
            {
                // Remove vote
                _context.ModRequestVotes.Remove(existingVote);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vote removed for mod {ModId} by {User}", modRequestId, username);
                return false; // Now not voted
            }
            else
            {
                // Add vote
                _context.ModRequestVotes.Add(new ModRequestVote
                {
                    ModRequestId = modRequestId,
                    Username = username,
                    VotedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vote added for mod {ModId} by {User}", modRequestId, username);
                return true; // Now voted
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling vote for mod {ModId} by {User}", modRequestId, username);
            throw;
        }
    }

    public async Task<bool> HasUserVotedAsync(int modRequestId, string username)
    {
        try
        {
            return await _context.ModRequestVotes
                .AnyAsync(v => v.ModRequestId == modRequestId && v.Username == username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking vote status for mod {ModId} by {User}", modRequestId, username);
            return false;
        }
    }

    public async Task<int> GetVoteCountAsync(int modRequestId)
    {
        try
        {
            return await _context.ModRequestVotes
                .CountAsync(v => v.ModRequestId == modRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vote count for mod {ModId}", modRequestId);
            return 0;
        }
    }

    public async Task<DateTime?> GetLastVoteResetDateAsync()
    {
        var value = await _rulesService.GetServerConfigAsync(VoteResetConfigKey);
        if (DateTime.TryParse(value, out var date))
        {
            return date;
        }
        return null;
    }

    public async Task<bool> ResetAllVotesAsync()
    {
        try
        {
            // Delete all votes
            await _context.ModRequestVotes.ExecuteDeleteAsync();

            // Update reset date
            await _rulesService.SetServerConfigAsync(VoteResetConfigKey, DateTime.UtcNow.ToString("O"));

            _logger.LogInformation("All mod request votes have been reset");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting all votes");
            return false;
        }
    }

    public async Task<ModMetadata?> FetchModMetadataAsync(string modName)
    {
        try
        {
            var apiUrl = $"https://mods.factorio.com/api/mods/{Uri.EscapeDataString(modName)}";
            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch mod metadata for {ModName}: {StatusCode}", modName, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var thumbnail = root.TryGetProperty("thumbnail", out var thumbProp) && thumbProp.ValueKind != JsonValueKind.Null
                ? thumbProp.GetString()
                : null;

            // Build full thumbnail URL if it's a relative path
            if (!string.IsNullOrEmpty(thumbnail) && !thumbnail.StartsWith("http"))
            {
                thumbnail = $"https://assets-mod.factorio.com{thumbnail}";
            }

            return new ModMetadata
            {
                Name = root.GetProperty("name").GetString() ?? modName,
                Title = root.GetProperty("title").GetString() ?? modName,
                Owner = root.TryGetProperty("owner", out var ownerProp) ? ownerProp.GetString() : null,
                Summary = root.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() : null,
                Thumbnail = thumbnail,
                DownloadsCount = root.TryGetProperty("downloads_count", out var downloadsProp) ? downloadsProp.GetInt32() : 0,
                Category = root.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching mod metadata for {ModName}", modName);
            return null;
        }
    }

    private static string? ExtractModNameFromUrl(string url)
    {
        // Match patterns like:
        // https://mods.factorio.com/mod/alien-biomes
        // https://mods.factorio.com/mod/alien-biomes/downloads
        var match = ModUrlRegex().Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    [GeneratedRegex(@"https?://mods\.factorio\.com/mod/([^/\s?]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ModUrlRegex();
}
