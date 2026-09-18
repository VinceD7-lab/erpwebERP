namespace erpWeb.Core.Audit;

/// <summary>Consultation du journal d'audit (l'écriture est assurée par l'intercepteur EF Core).</summary>
public interface ILectureJournalAudit
{
    Task<IReadOnlyList<EntreeJournalAuditDto>> ListerRecentesAsync(int nombreMaximum, CancellationToken jetonAnnulation = default);

    Task<IReadOnlyList<ActiviteJournaliere>> ObtenirActiviteAsync(int nombreJours, CancellationToken jetonAnnulation = default);
}
