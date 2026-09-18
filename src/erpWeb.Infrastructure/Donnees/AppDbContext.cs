using erpWeb.Core.Audit;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWeb.Core.Parametres;
using erpWeb.Core.Utilisateurs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Infrastructure.Donnees;

public sealed class AppDbContext : IdentityDbContext<Utilisateur>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EntreeJournalAudit> JournalAudit => Set<EntreeJournalAudit>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Parametre> Parametres => Set<Parametre>();

    DbSet<Utilisateur> IAppDbContext.Utilisateurs => Users;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
