namespace erpWeb.Core.Autorisation;

public static class RolesApplication
{
    public const string Administrateur = "Admin";
    public const string Utilisateur = "Utilisateur";

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PermissionsParRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Administrateur] = Permissions.Toutes,
            [Utilisateur] = [Permissions.Documents.Lire, Permissions.Documents.Deposer],
        };
}
