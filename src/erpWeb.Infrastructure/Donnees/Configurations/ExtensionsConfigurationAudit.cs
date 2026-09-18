using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal static class ExtensionsConfigurationAudit
{
    public const int LongueurNomUtilisateur = 256;

    public static EntityTypeBuilder<T> ConfigurerChampsAudit<T>(this EntityTypeBuilder<T> entite)
        where T : EntiteAuditable
    {
        entite.Property(e => e.CreePar).HasMaxLength(LongueurNomUtilisateur);
        entite.Property(e => e.ModifiePar).HasMaxLength(LongueurNomUtilisateur);
        return entite;
    }
}
