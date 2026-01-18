using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class DownloadLog
{
    public int Id { get; set; }

    [Required]
    public int SaveFileId { get; set; }

    // Navigation property
    public SaveFile SaveFile { get; set; } = null!;

    [Required]
    public DateTime DownloadDate { get; set; } = DateTime.UtcNow;

    // Only track authenticated users (GDPR-compliant)
    [StringLength(256)]
    public string? UserId { get; set; }

    // NO IP ADDRESS - GDPR compliance
    // NO USER AGENT - GDPR compliance
}
