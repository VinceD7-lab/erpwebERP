using Microsoft.AspNetCore.Identity;

namespace erpWeb.Core.Utilisateurs;

internal static class ExtensionsIdentite
{
    public static IReadOnlyList<string> MessagesErreur(this IdentityResult resultat)
        => resultat.Errors.Select(erreur => erreur.Description).ToList();
}
