using Microsoft.AspNetCore.Components.Forms;

namespace Madtorio.Services;

public interface IFileStorageService
{
    Task<(bool success, string? filePath, string? error)> SaveFileAsync(IBrowserFile file, string subdirectory = "saves");
    Task<bool> DeleteFileAsync(string filePath);
    string GetDownloadUrl(string filePath);
    bool ValidateFileType(string fileName, string[] allowedExtensions);
    string FormatFileSize(long bytes);
}
