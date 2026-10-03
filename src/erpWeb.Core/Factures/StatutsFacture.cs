namespace erpWeb.Core.Factures;

public static class StatutsFacture
{
    public const string Emise = "Emise";
    public const string Payee = "Payee";
    public const string EnRetard = "EnRetard";

    public static IReadOnlyList<string> Tous { get; } = [Emise, Payee, EnRetard];
}
