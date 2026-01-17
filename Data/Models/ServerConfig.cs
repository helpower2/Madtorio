using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class ServerConfig
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    [StringLength(256)]
    public string? ModifiedBy { get; set; }
}
