using erpWeb.Core.Utilisateurs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationUtilisateur : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.Property(utilisateur => utilisateur.NomComplet).HasMaxLength(100).IsRequired();
    }
}
