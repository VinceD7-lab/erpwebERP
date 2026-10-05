using FluentValidation;

namespace erpWeb.Core.PlanningAnalyses;

public sealed class ValidateurCasePlanning : AbstractValidator<CasePlanningDto>
{
    public ValidateurCasePlanning()
    {
        RuleFor(casePlanning => casePlanning.IdClient).GreaterThan(0).WithName("Client");
        RuleFor(casePlanning => casePlanning.TypeAnalyse)
            .Must(type => TypesAnalysePlanning.Tous.Contains(type))
            .WithMessage("Le type d'analyse doit être A, B, C ou D.")
            .WithName("Type d'analyse");
        RuleFor(casePlanning => casePlanning.Date)
            .Must(JoursOuvres.EstOuvre)
            .WithMessage("Un type d'analyse ne peut pas être planifié un week-end.")
            .WithName("Date");
    }
}

public sealed class ValidateurSauvegardePlanning : AbstractValidator<SauvegardePlanningDto>
{
    public const int AnneeMinimale = 2000;
    public const int AnneeMaximale = 2100;

    public ValidateurSauvegardePlanning()
    {
        RuleFor(planning => planning.Annee).InclusiveBetween(AnneeMinimale, AnneeMaximale).WithName("Année");
        RuleFor(planning => planning.Mois).InclusiveBetween(1, 12).WithName("Mois");
        RuleFor(planning => planning.Cases).NotNull().WithName("Cases");
        RuleForEach(planning => planning.Cases).SetValidator(new ValidateurCasePlanning());
        RuleFor(planning => planning)
            .Must(planning => planning.Cases.All(casePlanning => casePlanning.Date.Year == planning.Annee && casePlanning.Date.Month == planning.Mois))
            .WithMessage("Toutes les dates doivent appartenir au mois planifié.")
            .When(planning => planning.Cases is not null);
        RuleFor(planning => planning.Cases)
            .Must(cases => cases.Select(casePlanning => (casePlanning.IdClient, casePlanning.Date)).Distinct().Count() == cases.Count)
            .WithMessage("Un client ne peut avoir qu'un seul type d'analyse par jour.")
            .When(planning => planning.Cases is not null);
    }
}
