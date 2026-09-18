using erpWeb.Core.Audit;
using erpWeb.Core.Documents;
using erpWeb.Core.Parametres;
using erpWeb.Core.Utilisateurs;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Communs;

/// <summary>
/// Accès aux données utilisé par les services (pas de repositories).
/// N'expose volontairement ni Database ni ChangeTracker.
/// </summary>
public interface IAppDbContext
{
    DbSet<Utilisateur> Utilisateurs { get; }

    DbSet<EntreeJournalAudit> JournalAudit { get; }

    DbSet<Document> Documents { get; }

    DbSet<Parametre> Parametres { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
