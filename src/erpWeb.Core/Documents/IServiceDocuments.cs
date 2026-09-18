using erpWeb.Core.Communs;

namespace erpWeb.Core.Documents;

public interface IServiceDocuments
{
    Task<ResultatOperation<int>> DeposerAsync(DepotDocumentDto depot, CancellationToken jetonAnnulation = default);

    Task<IReadOnlyList<DocumentDto>> ListerAsync(string? typeEntite, string? idEntite, CancellationToken jetonAnnulation = default);

    Task<IReadOnlyList<DocumentDto>> ListerRecentsAsync(int nombreMaximum, CancellationToken jetonAnnulation = default);

    Task<FichierDocument?> OuvrirAsync(int id, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> SupprimerAsync(int id, CancellationToken jetonAnnulation = default);
}
