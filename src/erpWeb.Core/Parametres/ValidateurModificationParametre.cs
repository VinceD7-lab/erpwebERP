using FluentValidation;

namespace erpWeb.Core.Parametres;

public sealed class ValidateurModificationParametre : AbstractValidator<ModificationParametreDto>
{
    public ValidateurModificationParametre()
    {
        RuleFor(modification => modification.Cle)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[A-Za-z0-9]+(:[A-Za-z0-9]+)*$")
            .WithMessage("La clé doit suivre la notation Section:Cle (lettres et chiffres uniquement).")
            .WithName("Clé");

        RuleFor(modification => modification.Valeur).NotNull().MaximumLength(2000).WithName("Valeur");
        RuleFor(modification => modification.Description).MaximumLength(500).WithName("Description");
    }
}
