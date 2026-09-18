using ClosedXML.Excel;
using erpWeb.Core.Audit;

namespace erpWeb.Infrastructure.Exports;

public sealed class ExportJournalAuditExcel : IExportJournalAudit
{
    private static readonly string[] _entetes =
        ["Date (UTC)", "Utilisateur", "Action", "Type d'entité", "Identifiant", "Anciennes valeurs", "Nouvelles valeurs"];

    private static readonly double[] _largeursColonnes = [20, 28, 14, 20, 38, 60, 60];

    public string TypeContenu => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string ExtensionFichier => ".xlsx";

    public byte[] Exporter(IReadOnlyList<EntreeJournalAuditDto> entrees)
    {
        using var classeur = new XLWorkbook();
        var feuille = classeur.Worksheets.Add("Journal audit");

        for (var colonne = 0; colonne < _entetes.Length; colonne++)
        {
            feuille.Cell(1, colonne + 1).Value = _entetes[colonne];
            feuille.Column(colonne + 1).Width = _largeursColonnes[colonne];
        }

        feuille.Row(1).Style.Font.Bold = true;

        for (var index = 0; index < entrees.Count; index++)
        {
            var entree = entrees[index];
            var ligne = index + 2;
            feuille.Cell(ligne, 1).Value = entree.Date;
            feuille.Cell(ligne, 2).Value = entree.Utilisateur ?? string.Empty;
            feuille.Cell(ligne, 3).Value = LibelleAction(entree.Action);
            feuille.Cell(ligne, 4).Value = entree.TypeEntite;
            feuille.Cell(ligne, 5).Value = entree.IdEntite;
            feuille.Cell(ligne, 6).Value = entree.AnciennesValeurs ?? string.Empty;
            feuille.Cell(ligne, 7).Value = entree.NouvellesValeurs ?? string.Empty;
        }

        feuille.Column(1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
        feuille.Range(1, 1, Math.Max(entrees.Count + 1, 1), _entetes.Length).SetAutoFilter();
        feuille.SheetView.FreezeRows(1);

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }

    private static string LibelleAction(ActionAudit action) => action switch
    {
        ActionAudit.Creation => "Création",
        ActionAudit.Modification => "Modification",
        ActionAudit.Suppression => "Suppression",
        _ => action.ToString(),
    };
}
