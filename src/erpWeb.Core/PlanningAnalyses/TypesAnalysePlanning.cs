namespace erpWeb.Core.PlanningAnalyses;

/// <summary>
/// Types d'analyse planifiables : A = toutes les analyses, B, C et D = toutes sauf une, deux ou trois
/// (les analyses exclues sont choisies au hasard lors de l'application du planning).
/// </summary>
public static class TypesAnalysePlanning
{
    public const int Longueur = 1;

    public const string A = "A";
    public const string B = "B";
    public const string C = "C";
    public const string D = "D";

    public static IReadOnlyList<string> Tous { get; } = [A, B, C, D];
}
