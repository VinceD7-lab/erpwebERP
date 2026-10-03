using erpWeb.Core.Communs;

namespace erpWeb.Core.Factures;

public interface IRechercheFactures
{
    Task<ResultatPagine<FactureDto>> RechercherAsync(CriteresFactures criteres, CancellationToken jetonAnnulation = default);
}
