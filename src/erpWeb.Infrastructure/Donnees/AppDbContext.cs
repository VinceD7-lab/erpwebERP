using erpWeb.Core.Audit;
using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Parametres;
using erpWeb.Core.Tournees;
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

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<TourneeRamassage> TourneesRamassage => Set<TourneeRamassage>();

    public DbSet<Echantillon> Echantillons => Set<Echantillon>();

    public DbSet<ResultatAgronomie> ResultatsAgronomie => Set<ResultatAgronomie>();

    DbSet<Utilisateur> IAppDbContext.Utilisateurs => Users;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
