using Madtorio.Data.Models;

namespace Madtorio.Services;

public interface IModRequestService
{
    // Queries
    Task<List<ModRequest>> GetAllRequestsAsync(ModRequestStatus? statusFilter = null);
    Task<ModRequest?> GetRequestByIdAsync(int id);
    Task<ModRequest?> GetRequestByModNameAsync(string modName);

    // Request management with daily limit
    Task<int> GetUserRequestCountTodayAsync(string username);
    Task<bool> CanUserRequestTodayAsync(string username);
    Task<(ModRequest Request, bool IsNew)> CreateOrIncrementRequestAsync(string modUrl, string requestedBy);
    Task<bool> UpdateStatusAsync(int id, ModRequestStatus status);
    Task<bool> DeleteRequestAsync(int id);

    // Voting
    Task<bool> ToggleVoteAsync(int modRequestId, string username);
    Task<bool> HasUserVotedAsync(int modRequestId, string username);
    Task<int> GetVoteCountAsync(int modRequestId);

    // Vote reset (admin)
    Task<DateTime?> GetLastVoteResetDateAsync();
    Task<bool> ResetAllVotesAsync();

    // API
    Task<ModMetadata?> FetchModMetadataAsync(string modName);
}

public class ModMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public string? Summary { get; set; }
    public string? Thumbnail { get; set; }
    public int DownloadsCount { get; set; }
    public string? Category { get; set; }
}
