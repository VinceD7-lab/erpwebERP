using erpWeb.Core.Communs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.PlanningAnalyses;

public sealed class ServicePlanningAnalyses : IServicePlanningAnalyses
{
    private const string MessageClientInconnu = "Un des clients du planning n'existe plus.";
    private const string MessageConflit = "Le planning a été modifié par un autre utilisateur : rechargez la page puis recommencez.";

    private readonly IAppDbContext _contexte;
    private readonly IValidator<SauvegardePlanningDto> _validateur;
    private readonly TimeProvider _horloge;

    public ServicePlanningAnalyses(IAppDbContext contexte, IValidator<SauvegardePlanningDto> validateur, TimeProvider horloge)
    {
        _contexte = contexte;
        _validateur = validateur;
        _horloge = horloge;
    }

    public async Task<PlanningMensuelDto> ObtenirMoisAsync(int? annee, int? mois, CancellationToken jetonAnnulation = default)
    {
        var aujourdhui = DateOnly.FromDateTime(_horloge.GetUtcNow().UtcDateTime);
        var periodeValide = annee is >= ValidateurSauvegardePlanning.AnneeMinimale and <= ValidateurSauvegardePlanning.AnneeMaximale
            && mois is >= 1 and <= 12;
        var anneeRetenue = periodeValide ? annee!.Value : aujourdhui.Year;
        var moisRetenu = periodeValide ? mois!.Value : aujourdhui.Month;

        var premierJour = new DateOnly(anneeRetenue, moisRetenu, 1);
        var jours = Enumerable.Range(0, DateTime.DaysInMonth(anneeRetenue, moisRetenu))
            .Select(decalage => premierJour.AddDays(decalage))
            .Select(date => new JourPlanningDto(date, JoursOuvres.EstOuvre(date)))
            .ToList();

        var clients = await _contexte.Clients
            .AsNoTracking()
            .OrderBy(client => client.RaisonSociale)
            .Select(client => new ClientPlanningDto(client.Id, client.RaisonSociale))
            .ToListAsync(jetonAnnulation);

        var dernierJour = jours[^1].Date;
        var cases = await _contexte.PlanningAnalyses
            .AsNoTracking()
            .Where(planning => planning.DateAnalyse >= premierJour && planning.DateAnalyse <= dernierJour)
            .Select(planning => new CasePlanningDto { IdClient = planning.IdClient, Date = planning.DateAnalyse, TypeAnalyse = planning.TypeAnalyse })
            .ToListAsync(jetonAnnulation);

        return new PlanningMensuelDto(anneeRetenue, moisRetenu, jours, clients, cases);
    }

    public async Task<ResultatOperation> SauvegarderMoisAsync(SauvegardePlanningDto planning, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateur.ValidateAsync(planning, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation.Echec(validation.MessagesErreur());
        }

        var identifiantsDemandes = planning.Cases.Select(casePlanning => casePlanning.IdClient).Distinct().ToList();
        var clientsExistants = await _contexte.Clients
            .Where(client => identifiantsDemandes.Contains(client.Id))
            .CountAsync(jetonAnnulation);
        if (clientsExistants != identifiantsDemandes.Count)
        {
            return ResultatOperation.Echec(MessageClientInconnu);
        }

        var premierJour = new DateOnly(planning.Annee, planning.Mois, 1);
        var dernierJour = premierJour.AddMonths(1).AddDays(-1);
        var existants = await _contexte.PlanningAnalyses
            .Where(ligne => ligne.DateAnalyse >= premierJour && ligne.DateAnalyse <= dernierJour)
            .ToListAsync(jetonAnnulation);
        var existantsParCase = existants.ToDictionary(ligne => (ligne.IdClient, ligne.DateAnalyse));
        var casesDemandees = planning.Cases.Select(casePlanning => (casePlanning.IdClient, casePlanning.Date)).ToHashSet();

        _contexte.PlanningAnalyses.RemoveRange(existants.Where(ligne => !casesDemandees.Contains((ligne.IdClient, ligne.DateAnalyse))));

        foreach (var casePlanning in planning.Cases)
        {
            if (existantsParCase.TryGetValue((casePlanning.IdClient, casePlanning.Date), out var ligne))
            {
                ligne.TypeAnalyse = casePlanning.TypeAnalyse;
            }
            else
            {
                _contexte.PlanningAnalyses.Add(new PlanningAnalyse
                {
                    IdClient = casePlanning.IdClient,
                    DateAnalyse = casePlanning.Date,
                    TypeAnalyse = casePlanning.TypeAnalyse,
                });
            }
        }

        try
        {
            await _contexte.SaveChangesAsync(jetonAnnulation);
        }
        catch (DbUpdateException)
        {
            // Index unique (client, jour) : un autre utilisateur a enregistré une case entre la lecture et l'écriture.
            return ResultatOperation.Echec(MessageConflit);
        }

        return ResultatOperation.Succes();
    }
}
