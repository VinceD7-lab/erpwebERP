namespace erpWeb.Core.Echantillons;

/// <summary>Valeurs possibles du statut d'analyse d'un échantillon.</summary>
public static class StatutsAnalyse
{
    public const string Recu = "Recu";
    public const string EnCours = "EnCours";
    public const string Termine = "Termine";

    public static IReadOnlyList<string> Tous { get; } = [Recu, EnCours, Termine];
}
