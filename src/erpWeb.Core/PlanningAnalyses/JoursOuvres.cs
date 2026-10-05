namespace erpWeb.Core.PlanningAnalyses;

/// <summary>Un jour est ouvré du lundi au vendredi (les jours fériés ne sont pas gérés).</summary>
public static class JoursOuvres
{
    public static bool EstOuvre(DateOnly date)
        => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
