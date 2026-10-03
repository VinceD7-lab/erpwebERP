using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;

namespace erpWeb.Core.Factures;

/// <summary>
/// Facture d'un échantillon. Les coordonnées du client sont figées à l'émission :
/// modifier le client plus tard ne doit pas réécrire une facture déjà émise.
/// </summary>
public class Facture : EntiteAuditable
{
    public int IdEchantillon { get; set; }

    public Echantillon? Echantillon { get; set; }

    public int NumeroFacture { get; set; }

    public DateOnly DateFacture { get; set; }

    public DateOnly? DateEcheance { get; set; }

    public int IdClient { get; set; }

    public Client? Client { get; set; }

    public string NomClient { get; set; } = string.Empty;

    public string? AdresseFacturation { get; set; }

    public string? CodePostal { get; set; }

    public string? Ville { get; set; }

    public string? Pays { get; set; }

    public string? ModePaiement { get; set; }

    /// <summary>Montant hors taxe après remise.</summary>
    public decimal MontantHorsTaxe { get; set; }

    public decimal TauxTva { get; set; }

    public decimal MontantTva { get; set; }

    public decimal MontantToutesTaxesComprises { get; set; }

    /// <summary>Remise accordée, en pourcentage.</summary>
    public decimal? Remise { get; set; }

    public string Devise { get; set; } = string.Empty;

    public string StatutFacture { get; set; } = StatutsFacture.Emise;

    public DateOnly? DatePaiement { get; set; }

    public string? Commentaire { get; set; }
}
