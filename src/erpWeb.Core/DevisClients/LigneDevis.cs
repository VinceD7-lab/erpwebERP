using erpWeb.Core.Communs;

namespace erpWeb.Core.DevisClients;

public class LigneDevis : EntiteAuditable
{
    public int DevisId { get; set; }

    public Devis? Devis { get; set; }

    /// <summary>Ordre d'affichage.</summary>
    public int NumeroLigne { get; set; }

    public string Designation { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Quantite { get; set; }

    /// <summary>Par exemple « heure » ou « pièce ».</summary>
    public string? Unite { get; set; }

    public decimal PrixUnitaireHorsTaxe { get; set; }

    /// <summary>Remise de la ligne, en pourcentage.</summary>
    public decimal? TauxRemise { get; set; }

    /// <summary>Taux de TVA en pourcentage, par exemple 20,00.</summary>
    public decimal TauxTaxe { get; set; }

    /// <summary>Calculé par <see cref="ICalculateurDevis"/>.</summary>
    public decimal MontantHorsTaxe { get; set; }
}
