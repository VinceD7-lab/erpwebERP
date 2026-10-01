using Bogus;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Tournees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erpWeb.Infrastructure.Donnees;

/// <summary>
/// Génère des tournées de ramassage, des échantillons et des résultats d'agronomie fictifs et cohérents
/// entre eux, en environnement de développement. S'appuie sur les clients déjà présents.
/// Idempotent : ne fait rien dès qu'une tournée existe. La graine est fixe : les données sont reproductibles.
/// </summary>
public sealed class GenerateurEchantillonsDemonstration
{
    private const int Graine = 20260901;
    private const int JoursHistorique = 60;
    private const int JoursAvantAnalysesTerminees = 7;
    private const int NombreEchantillonsSansRattachement = 3;

    private static readonly string[] _filieres = ["Grandes cultures", "Elevage", "Maraichage", "Viticulture"];

    private readonly IAppDbContext _contexte;
    private readonly TimeProvider _horloge;
    private readonly ILogger<GenerateurEchantillonsDemonstration> _journal;

    public GenerateurEchantillonsDemonstration(
        IAppDbContext contexte,
        TimeProvider horloge,
        ILogger<GenerateurEchantillonsDemonstration> journal)
    {
        _contexte = contexte;
        _horloge = horloge;
        _journal = journal;
    }

    public async Task GenererAsync(int nombreTournees, CancellationToken jetonAnnulation = default)
    {
        if (await _contexte.TourneesRamassage.AnyAsync(jetonAnnulation))
        {
            return;
        }

        var identifiantsClients = await _contexte.Clients.Select(client => client.Id).ToListAsync(jetonAnnulation);
        if (identifiantsClients.Count == 0)
        {
            _journal.LogWarning("Aucun client : échantillons de démonstration non générés.");
            return;
        }

        // Instance dédiée (pas de Randomizer.Seed global) : pas d'état statique partagé.
        var faker = new Faker("fr") { Random = new Randomizer(Graine) };
        var maintenant = _horloge.GetUtcNow().UtcDateTime;
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        var numeroSequence = 0;

        var tournees = GenererTournees(faker, nombreTournees, aujourdhui);
        foreach (var tournee in tournees)
        {
            var clientsTournee = faker.PickRandom(identifiantsClients, Math.Min(faker.Random.Int(2, 5), identifiantsClients.Count)).ToList();
            foreach (var idClient in clientsTournee)
            {
                for (var index = faker.Random.Int(1, 4); index > 0; index--)
                {
                    var echantillon = GenererEchantillon(faker, tournee, idClient, ++numeroSequence, aujourdhui);
                    tournee.Echantillons.Add(echantillon);
                }
            }
        }

        await _contexte.TourneesRamassage.AddRangeAsync(tournees, jetonAnnulation);

        // Échantillons sans tournée ni client : couvrent les clés étrangères nullables.
        for (var index = 0; index < NombreEchantillonsSansRattachement; index++)
        {
            var echantillon = GenererEchantillon(faker, tournee: null, idClient: null, ++numeroSequence, aujourdhui);
            await _contexte.Echantillons.AddAsync(echantillon, jetonAnnulation);
        }

        await _contexte.SaveChangesAsync(jetonAnnulation);

        _journal.LogInformation(
            "{Tournees} tournées et {Echantillons} échantillons de démonstration générés.", tournees.Count, numeroSequence);
    }

    private static List<TourneeRamassage> GenererTournees(Faker faker, int nombreTournees, DateOnly aujourdhui)
    {
        // Dates distinctes, du jour le plus ancien au plus récent, uniquement en semaine.
        var datesPossibles = Enumerable.Range(0, JoursHistorique)
            .Select(decalage => aujourdhui.AddDays(-decalage))
            .Where(date => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();
        var dates = faker.PickRandom(datesPossibles, Math.Min(nombreTournees, datesPossibles.Count)).OrderBy(date => date);

        return dates
            .Select(date => new TourneeRamassage
            {
                DateTournee = date,
                NomChauffeur = $"{faker.Name.FirstName()} {faker.Name.LastName()}",
                ImmatriculationCamion = faker.Random.Replace("??-###-??").ToUpperInvariant(),
                StatutTemperature = faker.Random.Bool(0.8f) ? StatutsTemperature.Conforme : StatutsTemperature.Alerte,
            })
            .ToList();
    }

    private static Echantillon GenererEchantillon(Faker faker, TourneeRamassage? tournee, int? idClient, int numeroSequence, DateOnly aujourdhui)
    {
        var dateReference = tournee?.DateTournee ?? aujourdhui;
        var filiere = faker.PickRandom(_filieres);

        // Le prélèvement précède ou coïncide avec la tournée qui le ramasse.
        var datePrelevement = dateReference.ToDateTime(TimeOnly.MinValue)
            .AddDays(-faker.Random.Int(0, 2))
            .AddHours(faker.Random.Int(7, 17));

        // Température de réception cohérente avec le statut de la tournée.
        var temperature = tournee?.StatutTemperature == StatutsTemperature.Alerte
            ? faker.Random.Decimal(9, 15)
            : faker.Random.Decimal(2, 8);

        var statut = ChoisirStatut(faker, tournee, aujourdhui);
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = $"ECH-{dateReference:yyyyMMdd}-{numeroSequence:D5}",
            DatePrelevement = datePrelevement,
            Filiere = filiere,
            StatutAnalyse = statut,
            TemperatureReception = Math.Round(temperature, 2),
            IdClient = idClient,
        };

        if (statut == StatutsAnalyse.Termine && tournee is not null)
        {
            echantillon.ResultatAgronomie = GenererResultat(faker, filiere, tournee.DateTournee, aujourdhui);
        }

        return echantillon;
    }

    private static string ChoisirStatut(Faker faker, TourneeRamassage? tournee, DateOnly aujourdhui)
    {
        if (tournee is null)
        {
            return StatutsAnalyse.Recu;
        }

        // Les analyses d'anciennes tournées sont terminées ; les récentes sont encore en cours.
        return tournee.DateTournee <= aujourdhui.AddDays(-JoursAvantAnalysesTerminees)
            ? StatutsAnalyse.Termine
            : faker.PickRandom(StatutsAnalyse.Recu, StatutsAnalyse.EnCours);
    }

    private static ResultatAgronomie GenererResultat(Faker faker, string filiere, DateOnly dateTournee, DateOnly aujourdhui)
    {
        // Validation entre 1 et 5 jours après la tournée, jamais dans le futur.
        var dateValidation = dateTournee.AddDays(faker.Random.Int(1, 5));
        if (dateValidation > aujourdhui)
        {
            dateValidation = aujourdhui;
        }

        var resultat = new ResultatAgronomie
        {
            DateValidation = dateValidation.ToDateTime(TimeOnly.MinValue).AddHours(faker.Random.Int(8, 17)),
        };

        // Un support fourrager n'a pas d'analyse de sol, et inversement.
        if (filiere == "Elevage")
        {
            resultat.TypeSupport = "Fourrage";
            resultat.ValeurUclFourrage = Math.Round(faker.Random.Decimal(0.6m, 1.2m), 3);
        }
        else
        {
            resultat.TypeSupport = "Sol";
            resultat.PhSol = Math.Round(faker.Random.Decimal(4.5m, 8.5m), 2);
            resultat.MatiereOrganique = Math.Round(faker.Random.Decimal(1m, 6m), 3);
            resultat.PhosphoreP2O5 = Math.Round(faker.Random.Decimal(20m, 300m), 3);
            resultat.PotassiumK2O = Math.Round(faker.Random.Decimal(50m, 400m), 3);
            resultat.ReliquatAzoteN = Math.Round(faker.Random.Decimal(10m, 120m), 3);
        }

        return resultat;
    }
}
