using System.Text;
using Microsoft.EntityFrameworkCore;
using VersionadorGdoor.Data;
using VersionadorGdoor.Models;

namespace VersionadorGdoor.Services;

public class IniExportService
{
    public async Task<string> BuildAsync(VersioningDbContext db)
    {
        var categories = await db.Categories
            .Include(category => category.Items.OrderBy(item => item.SortOrder))
            .OrderBy(category => category.SortOrder)
            .ToListAsync();

        var builder = new StringBuilder();

        foreach (var category in categories)
        {
            builder.Append('[').Append(category.SectionKey).AppendLine("]");

            foreach (var item in category.Items.OrderBy(item => item.SortOrder))
            {
                builder
                    .Append(item.IniKey)
                    .Append('=')
                    .Append(item.IsChanged ? item.CurrentVersion.Trim() : "")
                    .AppendLine();
            }
        }

        return builder.ToString();
    }

    public async Task<ExportRecord> ExportAndResetAsync(VersioningDbContext db)
    {
        var content = await BuildAsync(db);
        var changedCount = await db.Items.CountAsync(item => item.IsChanged);
        var export = new ExportRecord
        {
            Content = content,
            ChangedItemCount = changedCount,
            CreatedAt = DateTime.UtcNow
        };

        db.ExportRecords.Add(export);
        db.HistoryEntries.Add(new HistoryEntry
        {
            Action = "export",
            Details = $"Arquivo versoes.ini gerado com {changedCount} versao(oes) preenchida(s)."
        });

        await db.Items
            .Where(item => item.IsChanged)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsChanged, false)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow));

        await db.SaveChangesAsync();
        return export;
    }
}
