using Microsoft.AspNetCore.Components.Forms;

namespace Madtorio.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private const long MaxFileSize = 524288000; // 500MB

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<(bool success, string? filePath, string? error)> SaveFileAsync(IBrowserFile file, string subdirectory = "saves")
    {
        try
        {
            // Validate file size
            if (file.Size > MaxFileSize)
            {
                return (false, null, $"File size exceeds the maximum allowed size of {FormatFileSize(MaxFileSize)}.");
            }

            // Validate file type
            if (!ValidateFileType(file.Name, new[] { ".zip" }))
            {
                return (false, null, "Only .zip files are allowed.");
            }

            // Create upload directory if it doesn't exist
            // Use 'AppData' directory for Docker compatibility (mapped to persistent volume)
            var uploadPath = Path.Combine(_environment.ContentRootPath, "AppData", "uploads", subdirectory);
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.Name);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var fullPath = Path.Combine(uploadPath, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await file.OpenReadStream(MaxFileSize).CopyToAsync(fileStream);
            }

            // Return just the filename for database storage
            _logger.LogInformation("File saved successfully: {FileName}", uniqueFileName);

            return (true, uniqueFileName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", file.Name);
            return (false, null, $"An error occurred while saving the file: {ex.Message}");
        }
    }

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            // Use 'AppData' directory for Docker compatibility (mapped to persistent volume)
            var fullPath = Path.Combine(_environment.ContentRootPath, "AppData", "uploads", "saves", fileName);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                return true;
            }

            _logger.LogWarning("File not found for deletion: {FileName}", fileName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return false;
        }
    }

    public string GetDownloadUrl(string filePathOrName)
    {
        // Extract filename if it's a path (for backwards compatibility)
        var fileName = Path.GetFileName(filePathOrName);
        // Return API endpoint URL instead of static file path
        return $"/api/downloads/{fileName}";
    }

    public bool ValidateFileType(string fileName, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
