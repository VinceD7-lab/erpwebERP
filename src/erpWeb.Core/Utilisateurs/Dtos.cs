namespace erpWeb.Core.Utilisateurs;

public sealed class UtilisateurDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string NomComplet { get; init; } = string.Empty;

    public bool EstActif { get; init; }

    public DateTime DateCreation { get; init; }

    public IReadOnlyList<string> Roles { get; set; } = [];
}

public sealed class CreationUtilisateurDto
{
    public string Email { get; set; } = string.Empty;

    public string NomComplet { get; set; } = string.Empty;

    public string MotDePasse { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}

public sealed class ModificationUtilisateurDto
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NomComplet { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}

public sealed class InscriptionDto
{
    public string Email { get; set; } = string.Empty;

    public string NomComplet { get; set; } = string.Empty;

    public string MotDePasse { get; set; } = string.Empty;

    public string ConfirmationMotDePasse { get; set; } = string.Empty;
}

public sealed class ReinitialisationMotDePasseDto
{
    public string Id { get; set; } = string.Empty;

    public string NouveauMotDePasse { get; set; } = string.Empty;
}
