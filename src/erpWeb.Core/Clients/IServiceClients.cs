using erpWeb.Core.Communs;

namespace erpWeb.Core.Clients;

public interface IServiceClients
{
    Task<IReadOnlyList<ClientDto>> ListerAsync(CancellationToken jetonAnnulation = default);

    Task<ClientDto?> ObtenirAsync(int id, CancellationToken jetonAnnulation = default);

    Task<ModificationClientDto?> ObtenirPourModificationAsync(int id, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation<int>> CreerAsync(CreationClientDto creation, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> ModifierAsync(ModificationClientDto modification, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> SupprimerAsync(int id, CancellationToken jetonAnnulation = default);
}
