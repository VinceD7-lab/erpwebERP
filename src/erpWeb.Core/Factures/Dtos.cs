namespace erpWeb.Core.Factures;

/// <summary>Facture telle qu'affichée dans la grille et sur la page d'impression.</summary>
public sealed record FactureDto(
    int IdFacture,
    int IdEchantillon,
    string CodeBarresAnonyme,
    string Filiere,
    int NumeroFacture,
    DateOnly DateFacture,
    DateOnly? DateEcheance,
    int CodeClient,
    string NomClient,
    string? AdresseFacturation,
    string? CodePostal,
    string? Ville,
    string? Pays,
    string? ModePaiement,
    decimal MontantHorsTaxe,
    decimal TauxTva,
    decimal MontantTva,
    decimal MontantToutesTaxesComprises,
    decimal? Remise,
    string Devise,
    string StatutFacture,
    DateOnly? DatePaiement,
    string? Commentaire)
{
    /// <summary>Montant avant remise, reconstitué à partir du montant remisé (pour l'affichage de la ligne).</summary>
    public decimal MontantHorsTaxeAvantRemise => Remise is > 0 and < 100
        ? Math.Round(MontantHorsTaxe / (1 - Remise.Value / 100m), 2, MidpointRounding.AwayFromZero)
        : MontantHorsTaxe;
}

/// <summary>Données de la page imprimable : la facture et l'identité de l'émetteur.</summary>
public sealed record ImpressionFactureDto(FactureDto Facture, string NomEmetteur, string AdresseEmetteur);

public enum ChampTriFactures
{
    DateFacture,
    NumeroFacture,
    Client,
    DateEcheance,
    MontantToutesTaxesComprises,
    StatutFacture,
}

/// <summary>
/// Critères de la grille des factures. Les valeurs hors bornes sont ramenées aux bornes par le service.
/// </summary>
public sealed record CriteresFactures
{
    public const int TaillePageParDefaut = 25;

    public const int TaillePageMaximum = 200;

    public int NumeroPage { get; init; } = 1;

    public int TaillePage { get; init; } = TaillePageParDefaut;

    /// <summary>Borne inférieure incluse de la date de facture.</summary>
    public DateOnly? DateFactureDebut { get; init; }

    /// <summary>Borne supérieure incluse de la date de facture.</summary>
    public DateOnly? DateFactureFin { get; init; }

    public int? IdClient { get; init; }

    public string? StatutFacture { get; init; }

    public ChampTriFactures Tri { get; init; } = ChampTriFactures.DateFacture;

    public bool TriDescendant { get; init; } = true;
}
