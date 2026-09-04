using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VersionadorGdoor.Data;
using VersionadorGdoor.Models;

namespace VersionadorGdoor.Pages;

public class IndexModel : PageModel
{
    private readonly VersioningDbContext _db;

    public IndexModel(VersioningDbContext db)
    {
        _db = db;
    }

    public List<VersionCategory> Categories { get; private set; } = [];
    public List<HistoryEntry> History { get; private set; } = [];
    public string MainVersion { get; private set; } = "0.0.0.0";
    public int ChangedCount { get; private set; }

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories
            .AsNoTracking()
            .Include(category => category.Items.OrderBy(item => item.SortOrder))
            .OrderBy(category => category.SortOrder)
            .ToListAsync();

        MainVersion = Categories
            .SelectMany(category => category.Items)
            .FirstOrDefault(item => item.IniKey.Equals("Gdoor", StringComparison.OrdinalIgnoreCase))
            ?.CurrentVersion ?? "0.0.0.0";

        ChangedCount = Categories.SelectMany(category => category.Items).Count(item => item.IsChanged);

        History = await _db.HistoryEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(10)
            .ToListAsync();
    }
}
