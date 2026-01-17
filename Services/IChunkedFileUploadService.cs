namespace Madtorio.Services;

public interface IChunkedFileUploadService
{
    /// <summary>
    /// Initiates a new chunked upload session
    /// </summary>
    Task<string> InitiateUploadAsync(string fileName, long fileSize, string uploadedBy);

    /// <summary>
    /// Uploads a single chunk
    /// </summary>
    Task<(bool success, string? error)> UploadChunkAsync(string uploadId, int chunkIndex, byte[] chunkData);

    /// <summary>
    /// Finalizes upload by reassembling chunks and moving to final location
    /// </summary>
    Task<(bool success, string? filePath, string? error)> FinalizeUploadAsync(string uploadId);

    /// <summary>
    /// Cancels an upload and cleans up temp files
    /// </summary>
    Task<bool> CancelUploadAsync(string uploadId);

    /// <summary>
    /// Gets upload session info for resuming
    /// </summary>
    Task<UploadSessionInfo?> GetUploadSessionAsync(string uploadId);
}

public class UploadSessionInfo
{
    public string UploadId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalChunks { get; set; }
    public List<int> UploadedChunks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}
