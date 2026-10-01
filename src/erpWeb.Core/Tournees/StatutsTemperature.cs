namespace erpWeb.Core.Tournees;

/// <summary>Valeurs possibles du statut de température d'une tournée.</summary>
public static class StatutsTemperature
{
    public const string Conforme = "Conforme";
    public const string Alerte = "Alerte";

    public static IReadOnlyList<string> Tous { get; } = [Conforme, Alerte];
}
