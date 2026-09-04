using System.ComponentModel.DataAnnotations;

namespace VersionadorGdoor.Models;

public class HistoryEntry
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Action { get; set; } = "";

    [MaxLength(160)]
    public string ItemKey { get; set; } = "";

    [MaxLength(160)]
    public string ItemName { get; set; } = "";

    [MaxLength(80)]
    public string? OldVersion { get; set; }

    [MaxLength(80)]
    public string? NewVersion { get; set; }

    [MaxLength(500)]
    public string Details { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
