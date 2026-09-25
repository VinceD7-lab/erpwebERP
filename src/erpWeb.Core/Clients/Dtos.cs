namespace erpWeb.Core.Clients;

/// <summary>Champs saisis dans les formulaires de création et de modification.</summary>
public interface IChampsClient
{
    string RaisonSociale { get; }

    string? Adresse1 { get; }

    string? Adresse2 { get; }

    string? CodePostal { get; }

    string? Ville { get; }

    string? Pays { get; }

    string? Telephone1 { get; }

    string? Telephone2 { get; }

    string? Email { get; }
}

public sealed class ClientDto
{
    public int Id { get; init; }

    public string RaisonSociale { get; init; } = string.Empty;

    public string? Adresse1 { get; init; }

    public string? Adresse2 { get; init; }

    public string? CodePostal { get; init; }

    public string? Ville { get; init; }

    public string? Pays { get; init; }

    public string? Telephone1 { get; init; }

    public string? Telephone2 { get; init; }

    public string? Email { get; init; }
}

public sealed class CreationClientDto : IChampsClient
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

public sealed class ModificationClientDto : IChampsClient
{
    public int Id { get; set; }

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
