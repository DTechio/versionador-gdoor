using Microsoft.EntityFrameworkCore;
using VersionadorGdoor.Data;
using VersionadorGdoor.Models;

namespace VersionadorGdoor.Services;

public static class SeedData
{
    public static async Task InitializeAsync(VersioningDbContext db)
    {
        Directory.CreateDirectory("App_Data");
        await db.Database.EnsureCreatedAsync();

        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var categories = new[]
        {
            new VersionCategory { SectionKey = "PRINCIPAIS", DisplayName = "Principais", SortOrder = 1 },
            new VersionCategory { SectionKey = "PDVS", DisplayName = "PDVs", SortOrder = 2 },
            new VersionCategory { SectionKey = "SECUNDARIOS", DisplayName = "Secundarios", SortOrder = 3 },
            new VersionCategory { SectionKey = "TERCEIROS", DisplayName = "Terceiros", SortOrder = 4 }
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        AddItems(db, categories[0], [
            ("Gdoor", "GDOOR", "4.0.0.692"),
            ("CTe", "CTe", "4.0.0.350"),
            ("DAV", "DAV", "4.0.1.342"),
            ("DFeMonitor", "DFeMonitor", "4.0.1.398"),
            ("Etiquetas", "Etiquetas", "4.0.1.150"),
            ("GBackup", "GBackup", "4.0.0.36"),
            ("GeraBoletos", "GeraBoletos", "4.0.0.128"),
            ("GdoorMigration", "GdoorMigration", "4.0.0.144"),
            ("GdoorFoDll", "gdoorFo.dll", "4.0.0.6"),
            ("GdoorGuard", "gdoorGuard", "4.0.0.34"),
            ("GdoorServer", "GdoorServer", "4.0.0.6"),
            ("GdoorUserON", "gdoorUserON", "4.0.0.12"),
            ("Gfood", "GFood", "4.0.1.426"),
            ("GfoodMonitor", "GFood_Monitor", "4.0.1.194"),
            ("GfoodSelfService", "SelfService", "4.0.0.36"),
            ("Habilita", "Habilita", "4.0.0.46"),
            ("HubServer", "hubserver", "4.0.15.38"),
            ("PDVServer", "PDV_Server", "4.0.2.446"),
            ("Relatorios", "Relatorios", "4.0.0.470")
        ]);

        AddItems(db, categories[1], [
            ("InstallPAF", "INSTALL_PAF", "4.0.1.528"),
            ("NFCe", "NFCe", "4.0.1.700"),
            ("NotaManual", "NotaManual", "4.0.1.550"),
            ("PreVendaGerencial", "PreVendaGerencial", "4.0.1.310"),
            ("SAT", "SAT", "4.0.1.500")
        ]);

        AddItems(db, categories[2], [
            ("ApiWhatsApp", "ApiWhatsApp", "1.0.0.10"),
            ("Atualizador", "Atualizador", "4.0.0.34"),
            ("Consulta", "Consulta", "4.0.0.38"),
            ("ConsultaCEP", "ConsultaCEP", "4.0.0.24"),
            ("ConversaoFirebird", "ConversaoFirebird", "4.0.0.16"),
            ("ConversorCompras", "ConversorCompras", "4.0.0.8"),
            ("DisplaySenhas", "DisplayDeSenhas", "4.0.0.10"),
            ("GerenciadorGContabil", "Gerenciador_GContabil", "4.0.0.26"),
            ("GView", "GView", "4.0.0.52"),
            ("Instalar", "Instalar", "4.0.0.16"),
            ("IntegradorImendes", "IntegradorImendes", "4.0.0.92"),
            ("Mensagens", "Mensagens", "4.0.6205.30994"),
            ("Promocoes", "Promocoes", "4.0.0.76"),
            ("Suporte", "Suporte", "4.0.0.66"),
            ("TabNCM", "TabNCM", "4.0.0.46")
        ]);

        AddItems(db, categories[3], [
            ("Ajuda", "Ajuda", "4.0.0.8"),
            ("Api", "Api", "4.0.0.6"),
            ("BlocoX1", "BlocoX1", "4.0.0.56"),
            ("Camera", "Camera", "4.0.0.2"),
            ("ConverteBanco", "ConverteBanco", "4.0.0.14"),
            ("ImageapiDll", "imageapi.dll", "4.0.0.4"),
            ("Teclado", "Teclado", "1.0.0.0")
        ]);

        db.HistoryEntries.Add(new HistoryEntry
        {
            Action = "seed",
            Details = "Base inicial criada a partir do Versoes.ini, completada com Versoes - Copia.ini e priorizando versoes do PDF."
        });

        await db.SaveChangesAsync();
    }

    private static void AddItems(VersioningDbContext db, VersionCategory category, IReadOnlyList<(string Key, string Name, string Version)> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            db.Items.Add(new VersionItem
            {
                CategoryId = category.Id,
                IniKey = item.Key,
                DisplayName = item.Name,
                CurrentVersion = item.Version,
                SortOrder = index + 1,
                IsChanged = false,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
