using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class SaveFile
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public long FileSize { get; set; }

    [StringLength(256)]
    public string? UploadedBy { get; set; }

    // Navigation property for download tracking
    public ICollection<DownloadLog> DownloadLogs { get; set; } = new List<DownloadLog>();
}
