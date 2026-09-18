using System.Globalization;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.TableauDeBord.Widgets;

public sealed class WidgetUtilisateursActifs : IWidgetTableauDeBord
{
    private readonly IAppDbContext _contexte;

    public WidgetUtilisateursActifs(IAppDbContext contexte)
    {
        _contexte = contexte;
    }

    public string Nom => "utilisateurs-actifs";

    public string Titre => "Utilisateurs actifs";

    public string Icone => "bi-people";

    public int Ordre => 10;

    public string? PermissionRequise => Permissions.Utilisateurs.Lire;

    public async Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default)
    {
        var nombreTotal = await _contexte.Utilisateurs.CountAsync(jetonAnnulation);
        var nombreActifs = await _contexte.Utilisateurs.CountAsync(utilisateur => utilisateur.EstActif, jetonAnnulation);

        return new DonneesIndicateur(
            nombreActifs.ToString(CultureInfo.CurrentCulture),
            nombreActifs > 1 ? "comptes actifs" : "compte actif",
            $"{nombreTotal} compte(s) au total");
    }
}
