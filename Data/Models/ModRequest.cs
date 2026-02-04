using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class ModRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string ModName { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string ModTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ModUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [StringLength(200)]
    public string? Author { get; set; }

    [StringLength(1000)]
    public string? Summary { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public int DownloadsCount { get; set; }

    [Required]
    [StringLength(100)]
    public string RequestedBy { get; set; } = string.Empty;

    public int RequestCount { get; set; } = 1;

    public ModRequestStatus Status { get; set; } = ModRequestStatus.Pending;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation property for votes
    public ICollection<ModRequestVote> Votes { get; set; } = new List<ModRequestVote>();
}

public enum ModRequestStatus
{
    Pending = 0,
    Approved = 1,
    Installed = 2,
    Rejected = 3
}
