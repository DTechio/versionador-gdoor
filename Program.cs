using System.Text;
using Microsoft.EntityFrameworkCore;
using VersionadorGdoor.Data;
using VersionadorGdoor.Models;
using VersionadorGdoor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<VersioningDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/versionador.db"));
builder.Services.AddScoped<IniExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapPost("/api/items/{id:int}/version", async (int id, VersionUpdateRequest request, VersioningDbContext db) =>
{
    var item = await db.Items.FirstOrDefaultAsync(versionItem => versionItem.Id == id);
    if (item is null)
    {
        return Results.NotFound();
    }

    var nextVersion = (request.Version ?? "").Trim();
    if (item.CurrentVersion == nextVersion)
    {
        return Results.Ok(ToItemResponse(item));
    }

    var oldVersion = item.CurrentVersion;
    item.CurrentVersion = nextVersion;
    item.IsChanged = true;
    item.UpdatedAt = DateTime.UtcNow;

    db.HistoryEntries.Add(new HistoryEntry
    {
        Action = "version",
        ItemKey = item.IniKey,
        ItemName = item.DisplayName,
        OldVersion = oldVersion,
        NewVersion = item.CurrentVersion,
        Details = "Alteracao manual salva e marcada para gerar no INI."
    });

    await db.SaveChangesAsync();
    return Results.Ok(ToItemResponse(item));
});

app.MapPost("/api/items/{id:int}/increment", async (int id, IncrementRequest request, VersioningDbContext db) =>
{
    if (request.Amount is not (1 or 2))
    {
        return Results.BadRequest(new { message = "Use incremento 1 ou 2." });
    }

    var item = await db.Items.FirstOrDefaultAsync(versionItem => versionItem.Id == id);
    if (item is null)
    {
        return Results.NotFound();
    }

    var parts = item.CurrentVersion.Split('.', StringSplitOptions.TrimEntries);
    if (parts.Length == 0 || !int.TryParse(parts[^1], out var lastPart))
    {
        return Results.BadRequest(new { message = "A versao atual nao permite incremento automatico." });
    }

    var oldVersion = item.CurrentVersion;
    parts[^1] = (lastPart + request.Amount).ToString();
    item.CurrentVersion = string.Join('.', parts);
    item.IsChanged = true;
    item.UpdatedAt = DateTime.UtcNow;

    db.HistoryEntries.Add(new HistoryEntry
    {
        Action = "increment",
        ItemKey = item.IniKey,
        ItemName = item.DisplayName,
        OldVersion = oldVersion,
        NewVersion = item.CurrentVersion,
        Details = $"+{request.Amount} aplicado ao ultimo numero da versao."
    });

    await db.SaveChangesAsync();
    return Results.Ok(ToItemResponse(item));
});

app.MapPost("/api/items/{id:int}/generation", async (int id, GenerationUpdateRequest request, VersioningDbContext db) =>
{
    var item = await db.Items.FirstOrDefaultAsync(versionItem => versionItem.Id == id);
    if (item is null)
    {
        return Results.NotFound();
    }

    item.IsChanged = request.IsChanged;
    item.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(ToItemResponse(item));
});

app.MapPost("/api/items", async (CreateItemRequest request, VersioningDbContext db) =>
{
    var displayName = (request.DisplayName ?? "").Trim();
    var iniKey = (request.IniKey ?? "").Trim();
    var currentVersion = (request.CurrentVersion ?? "").Trim();

    if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(iniKey))
    {
        return Results.BadRequest(new { message = "Informe nome e chave do INI." });
    }

    var category = await db.Categories.FirstOrDefaultAsync(itemCategory => itemCategory.Id == request.CategoryId);
    if (category is null)
    {
        return Results.BadRequest(new { message = "Categoria invalida." });
    }

    if (await db.Items.AnyAsync(item => item.IniKey == iniKey))
    {
        return Results.BadRequest(new { message = "Ja existe um item com essa chave do INI." });
    }

    var sortOrder = await db.Items
        .Where(item => item.CategoryId == category.Id)
        .Select(item => (int?)item.SortOrder)
        .MaxAsync() ?? 0;

    var newItem = new VersionItem
    {
        CategoryId = category.Id,
        DisplayName = displayName,
        IniKey = iniKey,
        CurrentVersion = currentVersion,
        SortOrder = sortOrder + 1,
        IsChanged = !string.IsNullOrWhiteSpace(currentVersion),
        UpdatedAt = DateTime.UtcNow
    };

    db.Items.Add(newItem);
    db.HistoryEntries.Add(new HistoryEntry
    {
        Action = "create",
        ItemKey = newItem.IniKey,
        ItemName = newItem.DisplayName,
        NewVersion = newItem.CurrentVersion,
        Details = $"Item criado em {category.SectionKey} com versao {(string.IsNullOrWhiteSpace(newItem.CurrentVersion) ? "sem versao informada" : newItem.CurrentVersion)}."
    });

    await db.SaveChangesAsync();
    return Results.Ok(ToItemResponse(newItem));
});

app.MapDelete("/api/items/{id:int}", async (int id, VersioningDbContext db) =>
{
    var item = await db.Items.Include(versionItem => versionItem.Category).FirstOrDefaultAsync(versionItem => versionItem.Id == id);
    if (item is null)
    {
        return Results.NotFound();
    }

    db.HistoryEntries.Add(new HistoryEntry
    {
        Action = "delete",
        ItemKey = item.IniKey,
        ItemName = item.DisplayName,
        OldVersion = item.CurrentVersion,
        Details = $"Item removido de {item.Category?.SectionKey ?? "categoria desconhecida"}."
    });
    db.Items.Remove(item);

    await db.SaveChangesAsync();
    return Results.Ok(new { item.Id });
});

app.MapPost("/api/reorder", async (List<ReorderItemRequest> request, VersioningDbContext db) =>
{
    var ids = request.Select(item => item.Id).ToHashSet();
    var categoryIds = request.Select(item => item.CategoryId).ToHashSet();

    if (!await db.Categories.AnyAsync(category => categoryIds.Contains(category.Id)))
    {
        return Results.BadRequest(new { message = "Categoria invalida." });
    }

    var items = await db.Items.Where(item => ids.Contains(item.Id)).ToListAsync();
    foreach (var requestItem in request)
    {
        var item = items.FirstOrDefault(existingItem => existingItem.Id == requestItem.Id);
        if (item is null)
        {
            continue;
        }

        item.CategoryId = requestItem.CategoryId;
        item.SortOrder = requestItem.SortOrder;
        item.UpdatedAt = DateTime.UtcNow;
    }

    db.HistoryEntries.Add(new HistoryEntry
    {
        Action = "reorder",
        Details = "Lista reordenada por arrastar e soltar."
    });

    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/preview", async (VersioningDbContext db, IniExportService exporter) =>
{
    var content = await exporter.BuildAsync(db);
    return Results.Text(content, "text/plain; charset=utf-8");
});

app.MapPost("/api/export", async (VersioningDbContext db, IniExportService exporter) =>
{
    var export = await exporter.ExportAndResetAsync(db);
    var bytes = Encoding.UTF8.GetBytes(export.Content);
    return Results.File(bytes, "text/plain; charset=utf-8", "versoes.ini");
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VersioningDbContext>();
    await SeedData.InitializeAsync(db);
}

app.Run();

static object ToItemResponse(VersionItem item) => new
{
    item.Id,
    item.CategoryId,
    item.IniKey,
    item.DisplayName,
    item.CurrentVersion,
    item.SortOrder,
    item.IsChanged
};

public sealed record VersionUpdateRequest(string? Version);
public sealed record IncrementRequest(int Amount);
public sealed record GenerationUpdateRequest(bool IsChanged);
public sealed record CreateItemRequest(string? DisplayName, string? IniKey, int CategoryId, string? CurrentVersion);
public sealed record ReorderItemRequest(int Id, int CategoryId, int SortOrder);
