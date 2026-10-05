using erpWeb.Core.Clients;
using erpWeb.Core.Communs;

namespace erpWeb.Core.PlanningAnalyses;

/// <summary>Type d'analyse planifié pour un client à une date précise (une ligne par case renseignée).</summary>
public class PlanningAnalyse : EntiteAuditable
{
    public int IdClient { get; set; }

    public Client? Client { get; set; }

    public DateOnly DateAnalyse { get; set; }

    /// <summary>Une des valeurs de <see cref="TypesAnalysePlanning"/>.</summary>
    public string TypeAnalyse { get; set; } = string.Empty;
}
