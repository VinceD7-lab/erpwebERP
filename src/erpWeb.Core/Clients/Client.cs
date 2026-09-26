using erpWeb.Core.Communs;

namespace erpWeb.Core.Clients;

public class Client : EntiteAuditable
{
    public string RaisonSociale { get; set; } = string.Empty;

    public string? Adresse1 { get; set; }

    public string? Adresse2 { get; set; }

    public string? CodePostal { get; set; }

    public string? Ville { get; set; }

    public string? Pays { get; set; }

    public string? Telephone1 { get; set; }

    public string? Telephone2 { get; set; }

    public string? Email { get; set; }
}
