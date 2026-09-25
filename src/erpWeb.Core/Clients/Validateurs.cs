using FluentValidation;

namespace erpWeb.Core.Clients;

/// <summary>Règles communes à la création et à la modification (interne : non enregistré en DI).</summary>
internal sealed class ValidateurChampsClient : AbstractValidator<IChampsClient>
{
    public ValidateurChampsClient()
    {
        RuleFor(client => client.RaisonSociale).NotEmpty().MaximumLength(LongueursClient.RaisonSociale).WithName("Raison sociale");
        RuleFor(client => client.Adresse1).MaximumLength(LongueursClient.Adresse).WithName("Adresse (ligne 1)");
        RuleFor(client => client.Adresse2).MaximumLength(LongueursClient.Adresse).WithName("Adresse (ligne 2)");
        RuleFor(client => client.CodePostal)
            .Matches("^[0-9]{5}$").WithMessage("Le code postal doit comporter 5 chiffres.")
            .When(client => !string.IsNullOrEmpty(client.CodePostal));
        RuleFor(client => client.Ville).MaximumLength(LongueursClient.Ville).WithName("Ville");
        RuleFor(client => client.Pays).MaximumLength(LongueursClient.Pays).WithName("Pays");
        RuleFor(client => client.Telephone1).MaximumLength(LongueursClient.Telephone).WithName("Téléphone 1");
        RuleFor(client => client.Telephone2).MaximumLength(LongueursClient.Telephone).WithName("Téléphone 2");
        RuleFor(client => client.Email)
            .EmailAddress().MaximumLength(LongueursClient.Email).WithName("Email")
            .When(client => !string.IsNullOrEmpty(client.Email));
    }
}

public sealed class ValidateurCreationClient : AbstractValidator<CreationClientDto>
{
    public ValidateurCreationClient()
    {
        Include(new ValidateurChampsClient());
    }
}

public sealed class ValidateurModificationClient : AbstractValidator<ModificationClientDto>
{
    public ValidateurModificationClient()
    {
        RuleFor(modification => modification.Id).GreaterThan(0);
        Include(new ValidateurChampsClient());
    }
}
