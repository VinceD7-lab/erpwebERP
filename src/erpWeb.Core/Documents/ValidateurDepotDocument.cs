using FluentValidation;
using Microsoft.Extensions.Options;

namespace erpWeb.Core.Documents;

public sealed class ValidateurDepotDocument : AbstractValidator<DepotDocumentDto>
{
    public ValidateurDepotDocument(IOptions<OptionsStockage> options)
    {
        var tailleMaximale = options.Value.TailleMaximaleOctets;

        RuleFor(depot => depot.TypeEntite).NotEmpty().MaximumLength(100).WithName("Type d'entité");
        RuleFor(depot => depot.IdEntite).NotEmpty().MaximumLength(100).WithName("Identifiant d'entité");
        RuleFor(depot => depot.NomFichier).NotEmpty().MaximumLength(255).WithName("Nom du fichier");
        RuleFor(depot => depot.TypeContenu).NotEmpty().MaximumLength(255).WithName("Type de contenu");
        RuleFor(depot => depot.Taille)
            .GreaterThan(0).WithMessage("Le fichier est vide.")
            .LessThanOrEqualTo(tailleMaximale).WithMessage($"Le fichier dépasse la taille maximale ({tailleMaximale / (1024 * 1024)} Mo).");
    }
}
