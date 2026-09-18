namespace erpWeb.Core.Autorisation;

/// <summary>
/// Permissions fines par module et par action, portées par des claims de rôle.
/// Chaque permission correspond à une policy d'autorisation du même nom.
/// </summary>
public static class Permissions
{
    public const string TypeClaim = "permission";

    public static class Utilisateurs
    {
        public const string Lire = "Utilisateurs.Lire";
        public const string Gerer = "Utilisateurs.Gerer";
    }

    public static class JournalAudit
    {
        public const string Lire = "JournalAudit.Lire";
        public const string Exporter = "JournalAudit.Exporter";
    }

    public static class Documents
    {
        public const string Lire = "Documents.Lire";
        public const string Deposer = "Documents.Deposer";
        public const string Supprimer = "Documents.Supprimer";
    }

    public static class Parametres
    {
        public const string Lire = "Parametres.Lire";
        public const string Modifier = "Parametres.Modifier";
    }

    public static IReadOnlyList<string> Toutes { get; } =
    [
        Utilisateurs.Lire,
        Utilisateurs.Gerer,
        JournalAudit.Lire,
        JournalAudit.Exporter,
        Documents.Lire,
        Documents.Deposer,
        Documents.Supprimer,
        Parametres.Lire,
        Parametres.Modifier,
    ];
}
