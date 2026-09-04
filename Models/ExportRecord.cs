namespace VersionadorGdoor.Models;

public class ExportRecord
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ChangedItemCount { get; set; }

    public string Content { get; set; } = "";
}
