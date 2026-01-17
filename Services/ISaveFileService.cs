using Madtorio.Data.Models;

namespace Madtorio.Services;

public interface ISaveFileService
{
    Task<List<SaveFile>> GetAllSavesAsync();
    Task<SaveFile?> GetSaveByIdAsync(int id);
    Task<SaveFile> CreateSaveAsync(SaveFile saveFile);
    Task<bool> UpdateSaveAsync(SaveFile saveFile);
    Task<bool> DeleteSaveAsync(int id);
    Task<(int totalSaves, long totalSize)> GetStatisticsAsync();
}
