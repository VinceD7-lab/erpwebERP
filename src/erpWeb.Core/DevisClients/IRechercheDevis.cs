using erpWeb.Core.Communs;

namespace erpWeb.Core.DevisClients;

public interface IRechercheDevis
{
    Task<ResultatPagine<DevisDto>> RechercherAsync(CriteresDevis criteres, CancellationToken jetonAnnulation = default);
}
