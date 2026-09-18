using FluentValidation;

namespace erpWeb.Core.Utilisateurs;

internal static class ReglesUtilisateur
{
    public const int LongueurMinimaleMotDePasse = 8;

    public static IRuleBuilderOptions<T, string> EmailValide<T>(this IRuleBuilder<T, string> regle)
        => regle.NotEmpty().EmailAddress().MaximumLength(256).WithName("Email");

    public static IRuleBuilderOptions<T, string> NomCompletValide<T>(this IRuleBuilder<T, string> regle)
        => regle.NotEmpty().MaximumLength(100).WithName("Nom complet");

    public static IRuleBuilderOptions<T, string> MotDePasseValide<T>(this IRuleBuilder<T, string> regle)
        => regle.NotEmpty().MinimumLength(LongueurMinimaleMotDePasse).WithName("Mot de passe");
}

public sealed class ValidateurCreationUtilisateur : AbstractValidator<CreationUtilisateurDto>
{
    public ValidateurCreationUtilisateur()
    {
        RuleFor(creation => creation.Email).EmailValide();
        RuleFor(creation => creation.NomComplet).NomCompletValide();
        RuleFor(creation => creation.MotDePasse).MotDePasseValide();
    }
}

public sealed class ValidateurModificationUtilisateur : AbstractValidator<ModificationUtilisateurDto>
{
    public ValidateurModificationUtilisateur()
    {
        RuleFor(modification => modification.Id).NotEmpty();
        RuleFor(modification => modification.Email).EmailValide();
        RuleFor(modification => modification.NomComplet).NomCompletValide();
    }
}

public sealed class ValidateurInscription : AbstractValidator<InscriptionDto>
{
    public ValidateurInscription()
    {
        RuleFor(inscription => inscription.Email).EmailValide();
        RuleFor(inscription => inscription.NomComplet).NomCompletValide();
        RuleFor(inscription => inscription.MotDePasse).MotDePasseValide();
        RuleFor(inscription => inscription.ConfirmationMotDePasse)
            .Equal(inscription => inscription.MotDePasse)
            .WithMessage("La confirmation ne correspond pas au mot de passe.");
    }
}

public sealed class ValidateurReinitialisationMotDePasse : AbstractValidator<ReinitialisationMotDePasseDto>
{
    public ValidateurReinitialisationMotDePasse()
    {
        RuleFor(reinitialisation => reinitialisation.Id).NotEmpty();
        RuleFor(reinitialisation => reinitialisation.NouveauMotDePasse).MotDePasseValide();
    }
}
