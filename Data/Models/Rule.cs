using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class Rule
{
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? DetailedDescription { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation property
    public RuleCategory? Category { get; set; }
}
