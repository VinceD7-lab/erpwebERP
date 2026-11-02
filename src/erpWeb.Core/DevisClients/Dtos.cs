namespace erpWeb.Core.DevisClients;

/// <summary>Devis tel qu'affiché dans la grille.</summary>
public sealed record DevisDto(
    int Id,
    string Numero,
    int ClientId,
    string RaisonSocialeClient,
    DateTime DateDevis,
    DateTime DateValidite,
    StatutDevis Statut,
    string Objet,
    string? Reference,
    string Devise,
    decimal MontantTotalHorsTaxe,
    decimal MontantTotalTaxe,
    decimal MontantTotalToutesTaxesComprises);

public enum ChampTriDevis
{
    Client,
    Numero,
    DateDevis,
    DateValidite,
    Statut,
    Objet,
    MontantTotalToutesTaxesComprises,
}

/// <summary>
/// Critères de la grille des devis. Les valeurs hors bornes sont ramenées aux bornes par le service.
/// Par défaut : ordre croissant de la raison sociale du client.
/// </summary>
public sealed record CriteresDevis
{
    public const int TaillePageParDefaut = 10;

    public const int TaillePageMaximum = 200;

    public int NumeroPage { get; init; } = 1;

    public int TaillePage { get; init; } = TaillePageParDefaut;

    /// <summary>Texte recherché dans le numéro, la raison sociale du client, l'objet et la référence.</summary>
    public string? Recherche { get; init; }

    public ChampTriDevis Tri { get; init; } = ChampTriDevis.Client;

    public bool TriDescendant { get; init; }
}
