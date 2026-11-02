using erpWeb.Core.DevisClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationLigneDevis : IEntityTypeConfiguration<LigneDevis>
{
    public void Configure(EntityTypeBuilder<LigneDevis> builder)
    {
        builder.ToTable("LignesDevis", table =>
        {
            table.HasCheckConstraint("CK_LignesDevis_TauxRemise", "[TauxRemise] IS NULL OR [TauxRemise] BETWEEN 0 AND 100");
        });
        builder.ConfigurerChampsAudit();
        builder.Property(ligne => ligne.Designation).HasMaxLength(LongueursDevis.Designation).IsRequired();
        builder.Property(ligne => ligne.Description).HasMaxLength(LongueursDevis.Description);
        builder.Property(ligne => ligne.Unite).HasMaxLength(LongueursDevis.Unite);
        builder.Property(ligne => ligne.Quantite).HasPrecision(18, 3);
        builder.Property(ligne => ligne.PrixUnitaireHorsTaxe).HasPrecision(18, 2);
        builder.Property(ligne => ligne.TauxRemise).HasPrecision(5, 2);
        builder.Property(ligne => ligne.TauxTaxe).HasPrecision(5, 2);
        builder.Property(ligne => ligne.MontantHorsTaxe).HasPrecision(18, 2);
        builder.HasIndex(ligne => new { ligne.DevisId, ligne.NumeroLigne }).IsUnique();

        builder.HasOne(ligne => ligne.Devis)
            .WithMany(devis => devis.Lignes)
            .HasForeignKey(ligne => ligne.DevisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
