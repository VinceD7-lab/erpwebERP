using erpWeb.Core.Factures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationFacture : IEntityTypeConfiguration<Facture>
{
    public void Configure(EntityTypeBuilder<Facture> builder)
    {
        builder.ToTable("Factures", table =>
        {
            table.HasCheckConstraint(
                "CK_Factures_Montants",
                "[MontantHorsTaxe] >= 0 AND [MontantTva] >= 0 AND [MontantToutesTaxesComprises] >= 0");
            table.HasCheckConstraint("CK_Factures_Remise", "[Remise] IS NULL OR [Remise] BETWEEN 0 AND 100");
        });
        builder.ConfigurerChampsAudit();
        builder.Property(facture => facture.DateFacture).HasColumnType("date");
        builder.Property(facture => facture.DateEcheance).HasColumnType("date");
        builder.Property(facture => facture.DatePaiement).HasColumnType("date");
        builder.Property(facture => facture.NomClient).HasMaxLength(LongueursFacture.NomClient).IsRequired();
        builder.Property(facture => facture.AdresseFacturation).HasMaxLength(LongueursFacture.AdresseFacturation);
        builder.Property(facture => facture.CodePostal).HasMaxLength(LongueursFacture.CodePostal);
        builder.Property(facture => facture.Ville).HasMaxLength(LongueursFacture.Ville);
        builder.Property(facture => facture.Pays).HasMaxLength(LongueursFacture.Pays);
        builder.Property(facture => facture.ModePaiement).HasMaxLength(LongueursFacture.ModePaiement);
        builder.Property(facture => facture.MontantHorsTaxe).HasPrecision(12, 2);
        builder.Property(facture => facture.TauxTva).HasPrecision(5, 2);
        builder.Property(facture => facture.MontantTva).HasPrecision(12, 2);
        builder.Property(facture => facture.MontantToutesTaxesComprises).HasPrecision(12, 2);
        builder.Property(facture => facture.Remise).HasPrecision(5, 2);
        builder.Property(facture => facture.Devise).HasMaxLength(LongueursFacture.Devise).IsFixedLength().IsRequired();
        builder.Property(facture => facture.StatutFacture).HasMaxLength(LongueursFacture.StatutFacture).IsRequired();
        builder.HasIndex(facture => facture.NumeroFacture).IsUnique();
        builder.HasIndex(facture => facture.IdEchantillon).IsUnique();
        builder.HasIndex(facture => facture.IdClient);
        builder.HasIndex(facture => facture.DateFacture);

        // Une seule facture par échantillon ; Restrict : supprimer un échantillon ou un client ne doit jamais effacer une facture.
        builder.HasOne(facture => facture.Echantillon)
            .WithOne()
            .HasForeignKey<Facture>(facture => facture.IdEchantillon)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(facture => facture.Client)
            .WithMany()
            .HasForeignKey(facture => facture.IdClient)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
