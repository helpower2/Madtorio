using Microsoft.AspNetCore.Mvc;
using Madtorio.Services;

namespace Madtorio.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ISaveFileService _saveFileService;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(
        IWebHostEnvironment environment,
        ISaveFileService saveFileService,
        ILogger<DownloadsController> logger)
    {
        _environment = environment;
        _saveFileService = saveFileService;
        _logger = logger;
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        try
        {
            // Sanitize filename to prevent directory traversal
            fileName = Path.GetFileName(fileName);

            // Verify file exists in database
            var saveFiles = await _saveFileService.GetAllSavesAsync();
            var saveFile = saveFiles.FirstOrDefault(s => s.FilePath == fileName);

            if (saveFile == null)
            {
                _logger.LogWarning("Download attempt for non-existent save: {FileName}", fileName);
                return NotFound("Save file not found");
            }

            // Get physical file path - use 'data' directory for Docker compatibility
            var filePath = Path.Combine(_environment.ContentRootPath, "data", "uploads", "saves", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Physical file missing for save: {FileName}", fileName);
                return NotFound("File not found on disk");
            }

            _logger.LogInformation("Download started: {FileName} ({FileSize} bytes)",
                saveFile.FileName, saveFile.FileSize);

            // Return file with original filename
            return PhysicalFile(filePath, "application/zip", saveFile.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
            return StatusCode(500, "Error downloading file");
        }
    }
}
