namespace erpWeb.Core.Factures;

public sealed class OptionsFacturation
{
    public const string Section = "Facturation";

    /// <summary>Tarif hors taxe d'un échantillon, par filière (comparaison insensible à la casse).</summary>
    public Dictionary<string, decimal> TarifsParFiliere { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Grandes cultures"] = 45m,
        ["Elevage"] = 38m,
        ["Maraichage"] = 52m,
        ["Viticulture"] = 60m,
    };

    /// <summary>Tarif appliqué à une filière absente de <see cref="TarifsParFiliere"/>.</summary>
    public decimal TarifParDefaut { get; set; } = 40m;

    /// <summary>Taux de TVA en pourcentage.</summary>
    public decimal TauxTva { get; set; } = 20m;

    /// <summary>Remise accordée à toutes les factures, en pourcentage (aucune par défaut).</summary>
    public decimal? RemisePourcentage { get; set; }

    public int DelaiEcheanceEnJours { get; set; } = 30;

    public string Devise { get; set; } = "EUR";

    public string ModePaiementParDefaut { get; set; } = "Virement";

    public string NomEmetteur { get; set; } = "erpWeb";

    public string AdresseEmetteur { get; set; } = string.Empty;
}
