using System.ComponentModel.DataAnnotations;

namespace VersionadorGdoor.Models;

public class VersionCategory
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string SectionKey { get; set; } = "";

    [MaxLength(80)]
    public string DisplayName { get; set; } = "";

    public int SortOrder { get; set; }

    public List<VersionItem> Items { get; set; } = [];
}
