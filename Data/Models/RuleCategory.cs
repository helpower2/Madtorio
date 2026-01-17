using System.ComponentModel.DataAnnotations;

namespace Madtorio.Data.Models;

public class RuleCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation property
    public ICollection<Rule> Rules { get; set; } = new List<Rule>();
}
