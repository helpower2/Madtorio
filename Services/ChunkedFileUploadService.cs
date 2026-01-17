using System.Collections.Concurrent;

namespace Madtorio.Services;

public class ChunkedFileUploadService : IChunkedFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ChunkedFileUploadService> _logger;
    private static readonly ConcurrentDictionary<string, UploadSessionInfo> _activeSessions = new();

    private const int ChunkSize = 10485760; // 10MB (safe for SignalR/WebSocket transport)
    private const long MaxFileSize = 524288000; // 500MB total

    public ChunkedFileUploadService(IWebHostEnvironment environment, ILogger<ChunkedFileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> InitiateUploadAsync(string fileName, long fileSize, string uploadedBy)
    {
        if (fileSize > MaxFileSize)
            throw new InvalidOperationException($"File exceeds {MaxFileSize / 1024 / 1024}MB limit");

        var uploadId = Guid.NewGuid().ToString("N");
        var totalChunks = (int)Math.Ceiling((double)fileSize / ChunkSize);

        var session = new UploadSessionInfo
        {
            UploadId = uploadId,
            FileName = fileName,
            FileSize = fileSize,
            TotalChunks = totalChunks,
            CreatedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy
        };

        _activeSessions[uploadId] = session;
        Directory.CreateDirectory(GetTempUploadPath(uploadId));

        _logger.LogInformation("Initiated upload {UploadId}: {FileName} ({FileSize} bytes, {TotalChunks} chunks)",
            uploadId, fileName, fileSize, totalChunks);

        return uploadId;
    }

    public async Task<(bool success, string? error)> UploadChunkAsync(string uploadId, int chunkIndex, byte[] chunkData)
    {
        if (!_activeSessions.TryGetValue(uploadId, out var session))
        {
            _logger.LogWarning("Upload session not found: {UploadId}", uploadId);
            return (false, "Upload session not found");
        }

        try
        {
            var chunkPath = GetChunkPath(uploadId, chunkIndex);
            _logger.LogDebug("Writing chunk {ChunkIndex} to {ChunkPath} ({ChunkSize} bytes)",
                chunkIndex, chunkPath, chunkData.Length);

            await File.WriteAllBytesAsync(chunkPath, chunkData);
            session.UploadedChunks.Add(chunkIndex);

            _logger.LogInformation("Chunk {ChunkIndex}/{TotalChunks} uploaded for {UploadId} ({ChunkSize} bytes)",
                chunkIndex + 1, session.TotalChunks, uploadId, chunkData.Length);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkIndex} for {UploadId}: {Message}",
                chunkIndex, uploadId, ex.Message);
            return (false, $"Chunk upload failed: {ex.Message}");
        }
    }

    public async Task<(bool success, string? filePath, string? error)> FinalizeUploadAsync(string uploadId)
    {
        if (!_activeSessions.TryGetValue(uploadId, out var session))
            return (false, null, "Upload session not found");

        try
        {
            if (session.UploadedChunks.Count != session.TotalChunks)
                return (false, null, $"Missing {session.TotalChunks - session.UploadedChunks.Count} chunks");

            _logger.LogInformation("Finalizing {UploadId}: Assembling {TotalChunks} chunks", uploadId, session.TotalChunks);

            var finalPath = GetFinalFilePath(session.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            using (var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (int i = 0; i < session.TotalChunks; i++)
                {
                    var chunkPath = GetChunkPath(uploadId, i);
                    var chunkData = await File.ReadAllBytesAsync(chunkPath);
                    await finalStream.WriteAsync(chunkData);
                }
            }

            var finalSize = new FileInfo(finalPath).Length;
            if (finalSize != session.FileSize)
            {
                File.Delete(finalPath);
                return (false, null, $"Size mismatch: expected {session.FileSize}, got {finalSize}");
            }

            Directory.Delete(GetTempUploadPath(uploadId), true);
            _activeSessions.TryRemove(uploadId, out _);

            // Return just the filename (not full path) for database storage
            var fileName = Path.GetFileName(finalPath);
            _logger.LogInformation("Finalized {UploadId}: {FileName}", uploadId, fileName);

            return (true, fileName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing {UploadId}", uploadId);
            return (false, null, $"Finalization failed: {ex.Message}");
        }
    }

    public async Task<bool> CancelUploadAsync(string uploadId)
    {
        if (!_activeSessions.TryGetValue(uploadId, out _))
            return false;

        try
        {
            var tempPath = GetTempUploadPath(uploadId);
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            _activeSessions.TryRemove(uploadId, out _);
            _logger.LogInformation("Cancelled upload {UploadId}", uploadId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling {UploadId}", uploadId);
            return false;
        }
    }

    public Task<UploadSessionInfo?> GetUploadSessionAsync(string uploadId)
    {
        _activeSessions.TryGetValue(uploadId, out var session);
        return Task.FromResult(session);
    }

    private string GetTempUploadPath(string uploadId) =>
        Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "temp", uploadId);

    private string GetChunkPath(string uploadId, int chunkIndex) =>
        Path.Combine(GetTempUploadPath(uploadId), $"chunk_{chunkIndex:D4}.tmp");

    private string GetFinalFilePath(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        // Changed from WebRootPath to ContentRootPath/App_Data to prevent hot reload triggers
        return Path.Combine(_environment.ContentRootPath, "App_Data", "uploads", "saves", uniqueFileName);
    }
}
