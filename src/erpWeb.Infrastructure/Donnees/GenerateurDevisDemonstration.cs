using Bogus;
using erpWeb.Core.Communs;
using erpWeb.Core.DevisClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erpWeb.Infrastructure.Donnees;

/// <summary>
/// Génère des devis fictifs (avec leurs lignes) répartis sur les clients déjà présents, en environnement de développement.
/// Idempotent : ne fait rien dès qu'un devis existe. La graine est fixe : les données sont reproductibles.
/// </summary>
public sealed class GenerateurDevisDemonstration
{
    private const int Graine = 20261029;
    private const int JoursHistorique = 120;
    private const int JoursValiditeParDefaut = 30;

    private static readonly string[] _unites = ["heure", "pièce", "jour", "forfait"];
    private static readonly decimal[] _tauxTaxe = [20m, 10m, 5.5m];

    private readonly IAppDbContext _contexte;
    private readonly ICalculateurDevis _calculateur;
    private readonly TimeProvider _horloge;
    private readonly ILogger<GenerateurDevisDemonstration> _journal;

    public GenerateurDevisDemonstration(
        IAppDbContext contexte,
        ICalculateurDevis calculateur,
        TimeProvider horloge,
        ILogger<GenerateurDevisDemonstration> journal)
    {
        _contexte = contexte;
        _calculateur = calculateur;
        _horloge = horloge;
        _journal = journal;
    }

    public async Task GenererAsync(int quantite, CancellationToken jetonAnnulation = default)
    {
        if (await _contexte.Devis.AnyAsync(jetonAnnulation))
        {
            return;
        }

        var identifiantsClients = await _contexte.Clients.Select(client => client.Id).ToListAsync(jetonAnnulation);
        if (identifiantsClients.Count == 0)
        {
            _journal.LogWarning("Aucun client : devis de démonstration non générés.");
            return;
        }

        // Instance dédiée (pas de Randomizer.Seed global) : pas d'état statique partagé.
        var faker = new Faker("fr") { Random = new Randomizer(Graine) };
        var aujourdhui = _horloge.GetUtcNow().UtcDateTime.Date;

        var devis = Enumerable.Range(1, quantite)
            .Select(sequence => GenererDevis(faker, faker.PickRandom(identifiantsClients), sequence, aujourdhui))
            .ToList();

        await _contexte.Devis.AddRangeAsync(devis, jetonAnnulation);
        await _contexte.SaveChangesAsync(jetonAnnulation);

        _journal.LogInformation("{Devis} devis de démonstration générés.", devis.Count);
    }

    private Devis GenererDevis(Faker faker, int clientId, int sequence, DateTime aujourdhui)
    {
        var dateDevis = aujourdhui.AddDays(-faker.Random.Int(0, JoursHistorique));
        var dateValidite = dateDevis.AddDays(JoursValiditeParDefaut);
        var statut = ChoisirStatut(faker, dateValidite, aujourdhui);

        var devis = new Devis
        {
            Numero = $"DEV-{dateDevis.Year}-{sequence:D5}",
            ClientId = clientId,
            DateDevis = dateDevis,
            DateValidite = dateValidite,
            Statut = statut,
            Objet = faker.Commerce.ProductName(),
            Reference = faker.Random.Bool(0.5f) ? $"CMD-{faker.Random.Int(1000, 9999)}" : null,
            Description = faker.Random.Bool(0.5f) ? faker.Lorem.Sentence() : null,
            ConditionsPaiement = "30 jours fin de mois",
            DelaiLivraison = $"{faker.Random.Int(1, 6)} semaines",
            TauxRemiseGlobale = faker.Random.Bool(0.2f) ? faker.PickRandom(5m, 10m) : null,
        };

        var nombreLignes = faker.Random.Int(1, 5);
        for (var numeroLigne = 1; numeroLigne <= nombreLignes; numeroLigne++)
        {
            devis.Lignes.Add(new LigneDevis
            {
                NumeroLigne = numeroLigne,
                Designation = faker.Commerce.ProductName(),
                Quantite = faker.Random.Int(1, 20),
                Unite = faker.PickRandom(_unites),
                PrixUnitaireHorsTaxe = Math.Round(faker.Random.Decimal(10m, 500m), 2),
                TauxRemise = faker.Random.Bool(0.2f) ? 5m : null,
                TauxTaxe = faker.PickRandom(_tauxTaxe),
            });
        }

        _calculateur.Calculer(devis);
        RenseignerDatesReponse(faker, devis, dateDevis, aujourdhui);
        return devis;
    }

    private static StatutDevis ChoisirStatut(Faker faker, DateTime dateValidite, DateTime aujourdhui)
    {
        // Un devis resté sans réponse après sa date de validité est expiré.
        var statut = faker.PickRandom(
            StatutDevis.Brouillon, StatutDevis.Envoye, StatutDevis.Envoye, StatutDevis.Accepte, StatutDevis.Refuse, StatutDevis.Annule);
        return statut is StatutDevis.Envoye && dateValidite < aujourdhui ? StatutDevis.Expire : statut;
    }

    private static void RenseignerDatesReponse(Faker faker, Devis devis, DateTime dateDevis, DateTime aujourdhui)
    {
        if (devis.Statut == StatutDevis.Brouillon)
        {
            return;
        }

        devis.DateEnvoi = dateDevis;
        if (devis.Statut is StatutDevis.Accepte or StatutDevis.Refuse)
        {
            var dateReponse = dateDevis.AddDays(faker.Random.Int(1, 15));
            devis.DateReponse = dateReponse > aujourdhui ? aujourdhui : dateReponse;
        }

        if (devis.Statut == StatutDevis.Refuse)
        {
            devis.MotifRefus = faker.PickRandom("Prix trop élevé", "Délai trop long", "Projet abandonné");
        }
    }
}
