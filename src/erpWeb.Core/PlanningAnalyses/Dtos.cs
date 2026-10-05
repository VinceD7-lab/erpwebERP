namespace erpWeb.Core.PlanningAnalyses;

public sealed record JourPlanningDto(DateOnly Date, bool EstOuvre);

public sealed record ClientPlanningDto(int Id, string RaisonSociale);

public sealed class CasePlanningDto
{
    public int IdClient { get; set; }

    public DateOnly Date { get; set; }

    public string TypeAnalyse { get; set; } = string.Empty;
}

public sealed record PlanningMensuelDto(
    int Annee,
    int Mois,
    IReadOnlyList<JourPlanningDto> Jours,
    IReadOnlyList<ClientPlanningDto> Clients,
    IReadOnlyList<CasePlanningDto> Cases);

/// <summary>Planning complet d'un mois : les cases absentes sont considérées comme vides.</summary>
public sealed class SauvegardePlanningDto
{
    public int Annee { get; set; }

    public int Mois { get; set; }

    public List<CasePlanningDto> Cases { get; set; } = [];
}
