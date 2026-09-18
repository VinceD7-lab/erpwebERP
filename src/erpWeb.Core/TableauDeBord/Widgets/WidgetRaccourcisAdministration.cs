using erpWeb.Core.Autorisation;

namespace erpWeb.Core.TableauDeBord.Widgets;

public sealed class WidgetRaccourcisAdministration : IWidgetTableauDeBord
{
    private static readonly DonneesRaccourcis _raccourcis = new(
    [
        new Raccourci("Nouvel utilisateur", "bi-person-plus", "/Utilisateurs/Creer", Permissions.Utilisateurs.Gerer),
        new Raccourci("Journal d'audit", "bi-journal-text", "/JournalAudit", Permissions.JournalAudit.Lire),
        new Raccourci("Déposer un document", "bi-upload", "/Documents/Deposer", Permissions.Documents.Deposer),
        new Raccourci("Paramètres", "bi-gear", "/Parametres", Permissions.Parametres.Lire),
    ]);

    public string Nom => "raccourcis";

    public string Titre => "Raccourcis";

    public string Icone => "bi-lightning";

    public int Ordre => 40;

    public string? PermissionRequise => null;

    public Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default)
        => Task.FromResult<DonneesWidget>(_raccourcis);
}
