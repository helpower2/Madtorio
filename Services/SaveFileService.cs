using Microsoft.EntityFrameworkCore;
using Madtorio.Data;
using Madtorio.Data.Models;

namespace Madtorio.Services;

public class SaveFileService : ISaveFileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SaveFileService> _logger;

    public SaveFileService(ApplicationDbContext context, ILogger<SaveFileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SaveFile>> GetAllSavesAsync()
    {
        try
        {
            return await _context.SaveFiles
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all save files");
            return new List<SaveFile>();
        }
    }

    public async Task<SaveFile?> GetSaveByIdAsync(int id)
    {
        try
        {
            return await _context.SaveFiles.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving save file with ID: {Id}", id);
            return null;
        }
    }

    public async Task<SaveFile> CreateSaveAsync(SaveFile saveFile)
    {
        try
        {
            _context.SaveFiles.Add(saveFile);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Save file created: {FileName}", saveFile.FileName);
            return saveFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating save file: {FileName}", saveFile.FileName);
            throw;
        }
    }

    public async Task<bool> UpdateSaveAsync(SaveFile saveFile)
    {
        try
        {
            _context.SaveFiles.Update(saveFile);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Save file updated: {Id}", saveFile.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating save file: {Id}", saveFile.Id);
            return false;
        }
    }

    public async Task<bool> DeleteSaveAsync(int id)
    {
        try
        {
            var saveFile = await _context.SaveFiles.FindAsync(id);
            if (saveFile == null)
            {
                _logger.LogWarning("Save file not found for deletion: {Id}", id);
                return false;
            }

            _context.SaveFiles.Remove(saveFile);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Save file deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting save file: {Id}", id);
            return false;
        }
    }

    public async Task<(int totalSaves, long totalSize)> GetStatisticsAsync()
    {
        try
        {
            var saves = await _context.SaveFiles.ToListAsync();
            return (saves.Count, saves.Sum(s => s.FileSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return (0, 0);
        }
    }
}
