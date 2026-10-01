using erpWeb.Core.Echantillons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationEchantillon : IEntityTypeConfiguration<Echantillon>
{
    public void Configure(EntityTypeBuilder<Echantillon> builder)
    {
        builder.ToTable("Echantillons", table => table.HasCheckConstraint(
            "CK_Echantillons_TemperatureReception",
            $"[TemperatureReception] IS NULL OR [TemperatureReception] BETWEEN {LongueursEchantillon.TemperatureMinimale} AND {LongueursEchantillon.TemperatureMaximale}"));
        builder.ConfigurerChampsAudit();
        builder.Property(echantillon => echantillon.CodeBarresAnonyme).HasMaxLength(LongueursEchantillon.CodeBarresAnonyme).IsRequired();
        builder.Property(echantillon => echantillon.Filiere).HasMaxLength(LongueursEchantillon.Filiere).IsRequired();
        builder.Property(echantillon => echantillon.StatutAnalyse).HasMaxLength(LongueursEchantillon.StatutAnalyse).IsRequired();
        builder.Property(echantillon => echantillon.TemperatureReception).HasPrecision(5, 2);
        builder.HasIndex(echantillon => echantillon.CodeBarresAnonyme).IsUnique();
        builder.HasIndex(echantillon => echantillon.IdTournee);
        builder.HasIndex(echantillon => echantillon.IdClient);

        // Restrict : supprimer une tournée ou un client ne doit jamais effacer silencieusement des échantillons.
        builder.HasOne(echantillon => echantillon.Tournee)
            .WithMany(tournee => tournee.Echantillons)
            .HasForeignKey(echantillon => echantillon.IdTournee)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(echantillon => echantillon.Client)
            .WithMany()
            .HasForeignKey(echantillon => echantillon.IdClient)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
