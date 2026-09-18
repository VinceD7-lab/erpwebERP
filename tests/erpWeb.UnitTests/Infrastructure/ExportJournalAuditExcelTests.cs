using ClosedXML.Excel;
using erpWeb.Core.Audit;
using erpWeb.Infrastructure.Exports;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class ExportJournalAuditExcelTests
{
    [Fact]
    public void Exporter_DeuxEntrees_ProduitUnClasseurAvecEnTeteEtUneLigneParEntree()
    {
        var entrees = new List<EntreeJournalAuditDto>
        {
            new() { Id = 1, Date = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc), Utilisateur = "admin@erpweb.local", Action = ActionAudit.Creation, TypeEntite = "Parametre", IdEntite = "1", NouvellesValeurs = "{\"Valeur\":\"25\"}" },
            new() { Id = 2, Date = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc), Action = ActionAudit.Suppression, TypeEntite = "Document", IdEntite = "4" },
        };

        var octets = new ExportJournalAuditExcel().Exporter(entrees);

        using var classeur = new XLWorkbook(new MemoryStream(octets));
        var feuille = classeur.Worksheet(1);
        Assert.Equal(3, feuille.LastRowUsed()!.RowNumber());
        Assert.Equal("Utilisateur", feuille.Cell(1, 2).GetString());
        Assert.Equal("Création", feuille.Cell(2, 3).GetString());
        Assert.Equal("Suppression", feuille.Cell(3, 3).GetString());
        Assert.Equal(string.Empty, feuille.Cell(3, 2).GetString());
    }
}
