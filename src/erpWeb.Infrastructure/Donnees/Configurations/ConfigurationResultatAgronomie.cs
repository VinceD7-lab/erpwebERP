using erpWeb.Core.Echantillons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationResultatAgronomie : IEntityTypeConfiguration<ResultatAgronomie>
{
    public void Configure(EntityTypeBuilder<ResultatAgronomie> builder)
    {
        builder.ToTable("ResultatsAgronomie");
        builder.ConfigurerChampsAudit();
        builder.Property(resultat => resultat.TypeSupport).HasMaxLength(LongueursEchantillon.TypeSupport);
        builder.Property(resultat => resultat.PhSol).HasPrecision(4, 2);
        builder.Property(resultat => resultat.MatiereOrganique).HasPrecision(6, 3);
        builder.Property(resultat => resultat.PhosphoreP2O5).HasPrecision(8, 3);
        builder.Property(resultat => resultat.PotassiumK2O).HasPrecision(8, 3);
        builder.Property(resultat => resultat.ReliquatAzoteN).HasPrecision(8, 3);
        builder.Property(resultat => resultat.ValeurUclFourrage).HasPrecision(6, 3);

        // Relation 1-1 : l'index unique sur IdEchantillon est créé par la relation.
        builder.HasOne(resultat => resultat.Echantillon)
            .WithOne(echantillon => echantillon.ResultatAgronomie)
            .HasForeignKey<ResultatAgronomie>(resultat => resultat.IdEchantillon)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
