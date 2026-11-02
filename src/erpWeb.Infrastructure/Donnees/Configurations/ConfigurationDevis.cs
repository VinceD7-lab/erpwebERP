using erpWeb.Core.DevisClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationDevis : IEntityTypeConfiguration<Devis>
{
    public void Configure(EntityTypeBuilder<Devis> builder)
    {
        builder.ToTable("Devis", table =>
        {
            table.HasCheckConstraint(
                "CK_Devis_Montants",
                "[MontantTotalHorsTaxe] >= 0 AND [MontantTotalTaxe] >= 0 AND [MontantTotalToutesTaxesComprises] >= 0");
            table.HasCheckConstraint("CK_Devis_TauxRemiseGlobale", "[TauxRemiseGlobale] IS NULL OR [TauxRemiseGlobale] BETWEEN 0 AND 100");
        });
        builder.ConfigurerChampsAudit();
        builder.Property(devis => devis.Numero).HasMaxLength(LongueursDevis.Numero).IsRequired();
        builder.Property(devis => devis.Statut).HasConversion<string>().HasMaxLength(LongueursDevis.Statut).IsRequired();
        builder.Property(devis => devis.Objet).HasMaxLength(LongueursDevis.Objet).IsRequired();
        builder.Property(devis => devis.Reference).HasMaxLength(LongueursDevis.Reference);
        builder.Property(devis => devis.Description).HasMaxLength(LongueursDevis.Description);
        builder.Property(devis => devis.NotesInternes).HasMaxLength(LongueursDevis.NotesInternes);
        builder.Property(devis => devis.ConditionsPaiement).HasMaxLength(LongueursDevis.ConditionsPaiement);
        builder.Property(devis => devis.DelaiLivraison).HasMaxLength(LongueursDevis.DelaiLivraison);
        builder.Property(devis => devis.Devise).HasMaxLength(LongueursDevis.Devise).IsFixedLength().IsRequired();
        builder.Property(devis => devis.MotifRefus).HasMaxLength(LongueursDevis.MotifRefus);
        builder.Property(devis => devis.MontantTotalHorsTaxe).HasPrecision(18, 2);
        builder.Property(devis => devis.MontantTotalTaxe).HasPrecision(18, 2);
        builder.Property(devis => devis.MontantTotalToutesTaxesComprises).HasPrecision(18, 2);
        builder.Property(devis => devis.TauxRemiseGlobale).HasPrecision(5, 2);
        builder.HasIndex(devis => devis.Numero).IsUnique();
        builder.HasIndex(devis => devis.ClientId);

        // Restrict : supprimer un client ne doit jamais effacer ses devis.
        builder.HasOne(devis => devis.Client)
            .WithMany()
            .HasForeignKey(devis => devis.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
