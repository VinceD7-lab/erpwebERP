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
