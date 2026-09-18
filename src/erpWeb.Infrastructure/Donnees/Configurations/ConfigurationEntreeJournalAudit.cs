using erpWeb.Core.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationEntreeJournalAudit : IEntityTypeConfiguration<EntreeJournalAudit>
{
    public void Configure(EntityTypeBuilder<EntreeJournalAudit> builder)
    {
        builder.ToTable("JournalAudit");
        builder.HasKey(entree => entree.Id);
        builder.Property(entree => entree.TypeEntite).HasMaxLength(100).IsRequired();
        builder.Property(entree => entree.IdEntite).HasMaxLength(100).IsRequired();
        builder.Property(entree => entree.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(entree => entree.Utilisateur).HasMaxLength(ExtensionsConfigurationAudit.LongueurNomUtilisateur);
        builder.HasIndex(entree => entree.Date);
        builder.HasIndex(entree => new { entree.TypeEntite, entree.IdEntite });
    }
}
