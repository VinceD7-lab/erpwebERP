using erpWeb.Core.Communs;

namespace erpWeb.Core.Audit;

/// <summary>Recherche paginée, filtrée et triée du journal d'audit (grille serveur).</summary>
public interface IRechercheJournalAudit
{
    Task<ResultatPagine<EntreeJournalAuditDto>> RechercherAsync(CriteresJournalAudit criteres, CancellationToken jetonAnnulation = default);
}
