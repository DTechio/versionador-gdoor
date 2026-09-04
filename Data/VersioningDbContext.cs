using Microsoft.EntityFrameworkCore;
using VersionadorGdoor.Models;

namespace VersionadorGdoor.Data;

public class VersioningDbContext(DbContextOptions<VersioningDbContext> options) : DbContext(options)
{
    public DbSet<VersionCategory> Categories => Set<VersionCategory>();
    public DbSet<VersionItem> Items => Set<VersionItem>();
    public DbSet<HistoryEntry> HistoryEntries => Set<HistoryEntry>();
    public DbSet<ExportRecord> ExportRecords => Set<ExportRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VersionCategory>()
            .HasIndex(category => category.SectionKey)
            .IsUnique();

        modelBuilder.Entity<VersionItem>()
            .HasIndex(item => item.IniKey)
            .IsUnique();

        modelBuilder.Entity<VersionItem>()
            .HasOne(item => item.Category)
            .WithMany(category => category.Items)
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
