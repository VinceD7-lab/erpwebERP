using System.Globalization;
using erpWeb.Core.Audit;
using erpWeb.Core.Autorisation;

namespace erpWeb.Core.TableauDeBord.Widgets;

public sealed class WidgetActiviteAudit : IWidgetTableauDeBord
{
    public const int NombreJours = 30;

    private readonly ILectureJournalAudit _lectureJournalAudit;

    public WidgetActiviteAudit(ILectureJournalAudit lectureJournalAudit)
    {
        _lectureJournalAudit = lectureJournalAudit;
    }

    public string Nom => "activite-audit";

    public string Titre => $"Activité des {NombreJours} derniers jours";

    public string Icone => "bi-activity";

    public int Ordre => 20;

    public string? PermissionRequise => Permissions.JournalAudit.Lire;

    public async Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default)
    {
        var activite = await _lectureJournalAudit.ObtenirActiviteAsync(NombreJours, jetonAnnulation);

        return new DonneesGraphique(
            "line",
            activite.Select(jour => jour.Jour.ToString("dd/MM", CultureInfo.InvariantCulture)).ToList(),
            [new SerieGraphique("Opérations", activite.Select(jour => jour.NombreOperations).ToList())]);
    }
}
