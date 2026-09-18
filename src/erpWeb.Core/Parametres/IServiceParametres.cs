using erpWeb.Core.Communs;

namespace erpWeb.Core.Parametres;

public interface IServiceParametres
{
    Task<IReadOnlyList<ParametreDto>> ListerAsync(CancellationToken jetonAnnulation = default);

    Task<ParametreDto?> ObtenirAsync(string cle, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> DefinirAsync(ModificationParametreDto modification, CancellationToken jetonAnnulation = default);
}
