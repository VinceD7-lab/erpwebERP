using erpWeb.Core.Parametres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationParametre : IEntityTypeConfiguration<Parametre>
{
    public const string NomTable = "Parametres";

    public void Configure(EntityTypeBuilder<Parametre> builder)
    {
        builder.ToTable(NomTable);
        builder.ConfigurerChampsAudit();
        builder.Property(parametre => parametre.Cle).HasMaxLength(200).IsRequired();
        builder.Property(parametre => parametre.Valeur).HasMaxLength(2000).IsRequired();
        builder.Property(parametre => parametre.Description).HasMaxLength(500);
        builder.HasIndex(parametre => parametre.Cle).IsUnique();
    }
}
