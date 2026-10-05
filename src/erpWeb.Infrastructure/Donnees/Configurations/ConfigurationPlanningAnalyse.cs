using erpWeb.Core.PlanningAnalyses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace erpWeb.Infrastructure.Donnees.Configurations;

internal sealed class ConfigurationPlanningAnalyse : IEntityTypeConfiguration<PlanningAnalyse>
{
    public void Configure(EntityTypeBuilder<PlanningAnalyse> builder)
    {
        var typesAutorises = string.Join(", ", TypesAnalysePlanning.Tous.Select(type => $"'{type}'"));
        builder.ToTable("PlanningAnalyses", table => table.HasCheckConstraint(
            "CK_PlanningAnalyses_TypeAnalyse",
            $"[TypeAnalyse] IN ({typesAutorises})"));
        builder.ConfigurerChampsAudit();
        builder.Property(planning => planning.DateAnalyse).HasColumnType("date");
        builder.Property(planning => planning.TypeAnalyse).HasMaxLength(TypesAnalysePlanning.Longueur).IsRequired();

        // Une seule case par client et par jour ; l'index sert aussi aux lectures d'un mois.
        builder.HasIndex(planning => new { planning.IdClient, planning.DateAnalyse }).IsUnique();
        builder.HasIndex(planning => planning.DateAnalyse);

        // Restrict : supprimer un client ne doit jamais effacer silencieusement son planning.
        builder.HasOne(planning => planning.Client)
            .WithMany()
            .HasForeignKey(planning => planning.IdClient)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
