using erpWeb.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationDocument : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.ConfigurerChampsAudit();
        builder.Property(document => document.TypeEntite).HasMaxLength(100).IsRequired();
        builder.Property(document => document.IdEntite).HasMaxLength(100).IsRequired();
        builder.Property(document => document.NomFichier).HasMaxLength(255).IsRequired();
        builder.Property(document => document.TypeContenu).HasMaxLength(255).IsRequired();
        builder.Property(document => document.CheminStockage).HasMaxLength(500).IsRequired();
        builder.HasIndex(document => new { document.TypeEntite, document.IdEntite });
        builder.HasIndex(document => document.DateCreation);
    }
}
