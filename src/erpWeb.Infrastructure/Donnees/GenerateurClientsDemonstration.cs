using Bogus;
using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace erpWeb.Infrastructure.Donnees;

/// <summary>
/// Génère des clients fictifs pour disposer de données de démonstration en environnement de développement.
/// Idempotent : complète jusqu'au nombre cible sans jamais dupliquer les clients déjà présents.
/// </summary>
public sealed class GenerateurClientsDemonstration
{
    private readonly IAppDbContext _contexte;
    private readonly IServiceClients _serviceClients;
    private readonly ILogger<GenerateurClientsDemonstration> _journal;

    public GenerateurClientsDemonstration(
        IAppDbContext contexte,
        IServiceClients serviceClients,
        ILogger<GenerateurClientsDemonstration> journal)
    {
        _contexte = contexte;
        _serviceClients = serviceClients;
        _journal = journal;
    }

    public async Task GenererAsync(int quantite, CancellationToken jetonAnnulation = default)
    {
        var nombreExistant = await _contexte.Clients.CountAsync(jetonAnnulation);
        var nombreAGenerer = quantite - nombreExistant;
        if (nombreAGenerer <= 0)
        {
            return;
        }

        var genererClient = new Faker<CreationClientDto>("fr")
            .RuleFor(client => client.RaisonSociale, faker => faker.Company.CompanyName())
            .RuleFor(client => client.Adresse1, faker => faker.Address.StreetAddress())
            .RuleFor(client => client.Adresse2, faker => faker.Random.Bool(0.3f) ? faker.Address.SecondaryAddress() : null)
            .RuleFor(client => client.CodePostal, faker => faker.Address.ZipCode("#####"))
            .RuleFor(client => client.Ville, faker => faker.Address.City())
            .RuleFor(client => client.Pays, _ => "France")
            .RuleFor(client => client.Telephone1, faker => faker.Phone.PhoneNumber("01########"))
            .RuleFor(client => client.Telephone2, faker => faker.Random.Bool(0.4f) ? faker.Phone.PhoneNumber("06########") : null)
            .RuleFor(client => client.Email, (faker, client) => $"contact-{faker.Random.AlphaNumeric(8)}@erpweb.local");

        var echecs = 0;
        foreach (var client in genererClient.Generate(nombreAGenerer))
        {
            var resultat = await _serviceClients.CreerAsync(client, jetonAnnulation);
            if (!resultat.Reussi)
            {
                echecs++;
                _journal.LogWarning("Client de démonstration rejeté : {Erreurs}", string.Join(" ", resultat.Erreurs));
            }
        }

        _journal.LogInformation(
            "{Quantite} clients de démonstration générés ({Echecs} échecs de validation).", nombreAGenerer - echecs, echecs);
    }
}
