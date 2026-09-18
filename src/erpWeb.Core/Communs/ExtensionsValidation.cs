using FluentValidation.Results;

namespace erpWeb.Core.Communs;

public static class ExtensionsValidation
{
    public static IReadOnlyList<string> MessagesErreur(this ValidationResult resultat)
        => resultat.Errors.Select(erreur => erreur.ErrorMessage).ToList();
}
