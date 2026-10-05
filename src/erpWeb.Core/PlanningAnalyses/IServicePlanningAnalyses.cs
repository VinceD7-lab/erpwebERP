using erpWeb.Core.Communs;

namespace erpWeb.Core.PlanningAnalyses;

public interface IServicePlanningAnalyses
{
    /// <summary>Planning d'un mois ; sans année ou mois valide, le mois courant est retourné.</summary>
    Task<PlanningMensuelDto> ObtenirMoisAsync(int? annee, int? mois, CancellationToken jetonAnnulation = default);

    /// <summary>Remplace le planning du mois : cases ajoutées, modifiées, ou supprimées si absentes.</summary>
    Task<ResultatOperation> SauvegarderMoisAsync(SauvegardePlanningDto planning, CancellationToken jetonAnnulation = default);
}
