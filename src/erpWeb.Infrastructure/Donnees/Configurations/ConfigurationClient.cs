using erpWeb.Core.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationClient : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.ConfigurerChampsAudit();
        builder.Property(client => client.RaisonSociale).HasMaxLength(LongueursClient.RaisonSociale).IsRequired();
        builder.Property(client => client.Adresse1).HasMaxLength(LongueursClient.Adresse);
        builder.Property(client => client.Adresse2).HasMaxLength(LongueursClient.Adresse);
        builder.Property(client => client.CodePostal).HasMaxLength(LongueursClient.CodePostal);
        builder.Property(client => client.Ville).HasMaxLength(LongueursClient.Ville);
        builder.Property(client => client.Pays).HasMaxLength(LongueursClient.Pays);
        builder.Property(client => client.Telephone1).HasMaxLength(LongueursClient.Telephone);
        builder.Property(client => client.Telephone2).HasMaxLength(LongueursClient.Telephone);
        builder.Property(client => client.Email).HasMaxLength(LongueursClient.Email);
        builder.HasIndex(client => client.RaisonSociale);
    }
}
