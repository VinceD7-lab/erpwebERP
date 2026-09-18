namespace erpWeb.Core.Audit;

public sealed class EntreeJournalAuditDto
{
    public long Id { get; init; }

    public string TypeEntite { get; init; } = string.Empty;

    public string IdEntite { get; init; } = string.Empty;

    public ActionAudit Action { get; init; }

    public string? Utilisateur { get; init; }

    public DateTime Date { get; init; }

    public string? AnciennesValeurs { get; init; }

    public string? NouvellesValeurs { get; init; }
}

public sealed record ActiviteJournaliere(DateOnly Jour, int NombreOperations);

/// <summary>Colonnes autorisées pour le tri : liste blanche, pas de tri dynamique par nom de propriété.</summary>
public enum ChampTriJournalAudit
{
    Date,
    Utilisateur,
    TypeEntite,
    Action,
}

/// <summary>
/// Critères de la grille du journal d'audit. Les valeurs hors bornes sont ramenées aux bornes
/// par le service : une grille ne doit pas échouer sur un numéro de page périmé.
/// </summary>
public sealed record CriteresJournalAudit
{
    public const int TaillePageParDefaut = 25;

    public const int TaillePageMaximum = 200;

    public int NumeroPage { get; init; } = 1;

    public int TaillePage { get; init; } = TaillePageParDefaut;

    /// <summary>Recherche libre sur l'utilisateur, le type d'entité et l'identifiant.</summary>
    public string? Recherche { get; init; }

    public ActionAudit? Action { get; init; }

    /// <summary>Borne inférieure incluse. DateTimeOffset : une chaîne ISO se lie sans ambiguïté de fuseau.</summary>
    public DateTimeOffset? DateDebut { get; init; }

    /// <summary>Borne supérieure exclue.</summary>
    public DateTimeOffset? DateFin { get; init; }

    public ChampTriJournalAudit Tri { get; init; } = ChampTriJournalAudit.Date;

    public bool TriDescendant { get; init; } = true;
}
