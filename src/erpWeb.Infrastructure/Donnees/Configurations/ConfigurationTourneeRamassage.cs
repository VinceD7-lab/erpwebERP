using erpWeb.Core.Tournees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationTourneeRamassage : IEntityTypeConfiguration<TourneeRamassage>
{
    public void Configure(EntityTypeBuilder<TourneeRamassage> builder)
    {
        builder.ToTable("TourneesRamassage");
        builder.ConfigurerChampsAudit();
        builder.Property(tournee => tournee.DateTournee).HasColumnType("date");
        builder.Property(tournee => tournee.NomChauffeur).HasMaxLength(LongueursTourneeRamassage.NomChauffeur).IsRequired();
        builder.Property(tournee => tournee.ImmatriculationCamion).HasMaxLength(LongueursTourneeRamassage.ImmatriculationCamion);
        builder.Property(tournee => tournee.StatutTemperature).HasMaxLength(LongueursTourneeRamassage.StatutTemperature);
        builder.HasIndex(tournee => tournee.DateTournee);
    }
}
