using erpWeb.Core.Clients;
using erpWeb.Core.Communs;

namespace erpWeb.Core.DevisClients;

/// <summary>Devis émis à un client ; les montants totaux sont calculés à partir des lignes (voir <see cref="ICalculateurDevis"/>).</summary>
public class Devis : EntiteAuditable
{
    public const string DeviseParDefaut = "EUR";

    /// <summary>Numéro unique, par exemple <c>DEV-2026-00042</c>.</summary>
    public string Numero { get; set; } = string.Empty;

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    /// <summary>Date d'émission.</summary>
    public DateTime DateDevis { get; set; }

    /// <summary>Date limite d'acceptation.</summary>
    public DateTime DateValidite { get; set; }

    public StatutDevis Statut { get; set; } = StatutDevis.Brouillon;

    public string Objet { get; set; } = string.Empty;

    /// <summary>Référence du client ou numéro de commande du demandeur.</summary>
    public string? Reference { get; set; }

    /// <summary>Texte libre ou notes visibles par le client.</summary>
    public string? Description { get; set; }

    /// <summary>Notes non imprimées sur le PDF.</summary>
    public string? NotesInternes { get; set; }

    public string? ConditionsPaiement { get; set; }

    public string? DelaiLivraison { get; set; }

    public string Devise { get; set; } = DeviseParDefaut;

    /// <summary>Somme des lignes, après remise globale.</summary>
    public decimal MontantTotalHorsTaxe { get; set; }

    public decimal MontantTotalTaxe { get; set; }

    public decimal MontantTotalToutesTaxesComprises { get; set; }

    /// <summary>Remise appliquée au devis entier, en pourcentage.</summary>
    public decimal? TauxRemiseGlobale { get; set; }

    /// <summary>Renseignée au passage à <see cref="StatutDevis.Envoye"/>.</summary>
    public DateTime? DateEnvoi { get; set; }

    /// <summary>Date d'acceptation ou de refus.</summary>
    public DateTime? DateReponse { get; set; }

    /// <summary>Utile pour l'analyse commerciale.</summary>
    public string? MotifRefus { get; set; }

    public List<LigneDevis> Lignes { get; set; } = [];
}
