using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class ModRequestLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public int ModRequestId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ModRequest? ModRequest { get; set; }
}
