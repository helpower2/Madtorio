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
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", subdirectory);
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

            // Return relative path for database storage
            var relativePath = Path.Combine("uploads", subdirectory, uniqueFileName).Replace("\\", "/");
            _logger.LogInformation("File saved successfully: {FilePath}", relativePath);

            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", file.Name);
            return (false, null, $"An error occurred while saving the file: {ex.Message}");
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, filePath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }

            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public string GetDownloadUrl(string filePath)
    {
        // Return URL path (without leading slash if filePath already has it)
        return filePath.StartsWith("/") ? filePath : $"/{filePath}";
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
