using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class ModRequestVote
{
    public int Id { get; set; }

    public int ModRequestId { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ModRequest? ModRequest { get; set; }
}
