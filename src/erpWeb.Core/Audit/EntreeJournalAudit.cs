namespace erpWeb.Core.Audit;

public class EntreeJournalAudit
{
    public long Id { get; set; }

    public string TypeEntite { get; set; } = string.Empty;

    public string IdEntite { get; set; } = string.Empty;

    public ActionAudit Action { get; set; }

    public string? Utilisateur { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Valeurs avant modification, sérialisées en JSON.</summary>
    public string? AnciennesValeurs { get; set; }

    /// <summary>Valeurs après modification, sérialisées en JSON.</summary>
    public string? NouvellesValeurs { get; set; }
}
