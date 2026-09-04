using System.ComponentModel.DataAnnotations;

namespace VersionadorGdoor.Models;

public class VersionItem
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string IniKey { get; set; } = "";

    [MaxLength(160)]
    public string DisplayName { get; set; } = "";

    [MaxLength(80)]
    public string CurrentVersion { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsChanged { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }

    public VersionCategory? Category { get; set; }
}
