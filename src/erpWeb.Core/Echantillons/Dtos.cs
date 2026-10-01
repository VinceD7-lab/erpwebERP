namespace erpWeb.Core.Echantillons;

/// <summary>Ligne de la grille des résultats d'échantillons.</summary>
public sealed record ResultatEchantillonDto(
    int IdEchantillon,
    DateOnly? DateTournee,
    int? IdClient,
    string? RaisonSocialeClient,
    string CodeBarresAnonyme,
    string Filiere,
    string StatutAnalyse,
    decimal? TemperatureReception,
    string? TypeSupport,
    decimal? PhSol,
    decimal? MatiereOrganique,
    decimal? PhosphoreP2O5,
    decimal? PotassiumK2O,
    decimal? ReliquatAzoteN,
    decimal? ValeurUclFourrage,
    DateTime? DateValidation);

public enum ChampTriResultatsEchantillons
{
    DateTournee,
    Client,
    CodeBarres,
    Filiere,
    StatutAnalyse,
}

/// <summary>
/// Critères de la grille des résultats d'échantillons. Les valeurs hors bornes sont ramenées
/// aux bornes par le service : une grille ne doit pas échouer sur un numéro de page périmé.
/// </summary>
public sealed record CriteresResultatsEchantillons
{
    public const int TaillePageParDefaut = 25;

    public const int TaillePageMaximum = 200;

    public int NumeroPage { get; init; } = 1;

    public int TaillePage { get; init; } = TaillePageParDefaut;

    /// <summary>Borne inférieure incluse de la date de tournée.</summary>
    public DateOnly? DateTourneeDebut { get; init; }

    /// <summary>Borne supérieure incluse de la date de tournée.</summary>
    public DateOnly? DateTourneeFin { get; init; }

    public int? IdClient { get; init; }

    public string? StatutAnalyse { get; init; }

    public ChampTriResultatsEchantillons Tri { get; init; } = ChampTriResultatsEchantillons.DateTournee;

    public bool TriDescendant { get; init; } = true;
}
