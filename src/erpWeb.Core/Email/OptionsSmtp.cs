namespace erpWeb.Core.Email;

public sealed class OptionsSmtp
{
    public const string Section = "Smtp";

    /// <summary>Serveur SMTP. Vide : les emails sont uniquement journalisés (développement).</summary>
    public string Hote { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UtiliserStartTls { get; set; } = true;

    public string? NomUtilisateur { get; set; }

    public string? MotDePasse { get; set; }

    public string AdresseExpediteur { get; set; } = "noreply@erpweb.local";

    public string NomExpediteur { get; set; } = "erpWeb";
}
