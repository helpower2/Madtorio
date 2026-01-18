using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class PageView
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string PagePath { get; set; } = string.Empty;

    [Required]
    public DateTime ViewDate { get; set; } = DateTime.UtcNow;

    // Only track authenticated users (GDPR-compliant)
    [StringLength(256)]
    public string? UserId { get; set; }

    // NO IP ADDRESS - GDPR compliance
    // NO USER AGENT - GDPR compliance
}
