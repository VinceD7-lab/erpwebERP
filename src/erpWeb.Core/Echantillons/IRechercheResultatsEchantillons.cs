using erpWeb.Core.Communs;

namespace erpWeb.Core.Echantillons;

/// <summary>Recherche paginée des résultats d'échantillons par date de tournée et par client (grille serveur).</summary>
public interface IRechercheResultatsEchantillons
{
    Task<ResultatPagine<ResultatEchantillonDto>> RechercherAsync(CriteresResultatsEchantillons criteres, CancellationToken jetonAnnulation = default);
}
