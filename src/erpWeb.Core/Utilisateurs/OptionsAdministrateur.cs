namespace erpWeb.Core.Utilisateurs;

/// <summary>
/// Compte administrateur créé au démarrage s'il n'existe pas.
/// Le mot de passe est lu depuis les secrets utilisateur, jamais depuis un fichier commité.
/// </summary>
public sealed class OptionsAdministrateur
{
    public const string Section = "Administrateur";

    public string Email { get; set; } = "admin@erpweb.local";

    public string NomComplet { get; set; } = "Administrateur";

    public string MotDePasse { get; set; } = string.Empty;
}
