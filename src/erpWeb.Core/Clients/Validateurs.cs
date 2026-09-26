using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentValidation;

namespace erpWeb.Core.Clients;

/// <summary>Règles communes à la création et à la modification (interne : non enregistré en DI).</summary>
internal sealed partial class ValidateurChampsClient : AbstractValidator<IChampsClient>
{
    private const string PaysParDefaut = "France";

    public ValidateurChampsClient()
    {
        RuleFor(client => client.RaisonSociale).NotEmpty().MaximumLength(LongueursClient.RaisonSociale).WithName("Raison sociale");
        RuleFor(client => client.Adresse1).MaximumLength(LongueursClient.Adresse).WithName("Adresse (ligne 1)");
        RuleFor(client => client.Adresse2).MaximumLength(LongueursClient.Adresse).WithName("Adresse (ligne 2)");

        RuleFor(client => client.CodePostal).MaximumLength(LongueursClient.CodePostal).WithName("Code postal");
        RuleFor(client => client.CodePostal)
            .Matches("^[0-9]{5}$").WithMessage("Le code postal français doit comporter 5 chiffres.")
            .When(client => !string.IsNullOrEmpty(client.CodePostal) && EstEnFrance(client.Pays));
        RuleFor(client => client.CodePostal)
            .Matches("^[A-Za-z0-9][A-Za-z0-9 -]*$").WithMessage("Le code postal ne peut contenir que des lettres, des chiffres, des espaces et des tirets.")
            .When(client => !string.IsNullOrEmpty(client.CodePostal) && !EstEnFrance(client.Pays));

        RuleFor(client => client.Ville).MaximumLength(LongueursClient.Ville).WithName("Ville");
        RuleFor(client => client.Pays).MaximumLength(LongueursClient.Pays).WithName("Pays");
        ValiderTelephone(client => client.Telephone1, "Téléphone 1");
        ValiderTelephone(client => client.Telephone2, "Téléphone 2");
        RuleFor(client => client.Email)
            .EmailAddress().MaximumLength(LongueursClient.Email).WithName("Email")
            .When(client => !string.IsNullOrEmpty(client.Email));
    }

    /// <summary>Un pays non renseigné désigne la France, clientèle par défaut.</summary>
    private static bool EstEnFrance(string? pays)
        => string.IsNullOrWhiteSpace(pays) || string.Equals(pays.Trim(), PaysParDefaut, StringComparison.OrdinalIgnoreCase);

    /// <summary>Valide le numéro tel qu'il sera enregistré, espaces et points retirés.</summary>
    private void ValiderTelephone(Expression<Func<IChampsClient, string?>> telephone, string nom)
        => RuleFor(telephone)
            .Must(EstTelephoneValide)
            .WithMessage($"{{PropertyName}} ne peut contenir que des chiffres, précédés éventuellement de « + » ({LongueursClient.Telephone} caractères au plus, hors espaces et points).")
            .WithName(nom);

    private static bool EstTelephoneValide(string? telephone)
    {
        var normalise = NormalisationTelephone.Normaliser(telephone);
        return normalise is null || (normalise.Length <= LongueursClient.Telephone && ExpressionTelephone().IsMatch(normalise));
    }

    [GeneratedRegex(@"^\+?[0-9]+$")]
    private static partial Regex ExpressionTelephone();
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
