using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceGenerationFacturesTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();
    private int _sequence;

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task GenererManquantesAsync_BaseVide_NeCreeAucuneFacture()
    {
        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(0, creees);
        Assert.Empty(await _base.Contexte.Factures.ToListAsync());
    }

    [Fact]
    public async Task GenererManquantesAsync_UnEchantillonParClient_CreeUneFacturePourChacun()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var premier = await AjouterEchantillonAsync(acier, "Elevage");
        var second = await AjouterEchantillonAsync(zinc, "Viticulture");

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(2, creees);
        var factures = await _base.Contexte.Factures.AsNoTracking().OrderBy(facture => facture.IdEchantillon).ToListAsync();
        Assert.Equal([premier.Id, second.Id], factures.Select(facture => facture.IdEchantillon).ToArray());
        Assert.Equal([acier.Id, zinc.Id], factures.Select(facture => facture.IdClient).ToArray());
        Assert.Equal([38m, 60m], factures.Select(facture => facture.MontantHorsTaxe).ToArray());
        Assert.Equal(["Acier SA", "Zinc SARL"], factures.Select(facture => facture.NomClient).ToArray());
    }

    [Fact]
    public async Task GenererManquantesAsync_EchantillonSansClient_EstIgnore()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(client: null);
        var facture = await AjouterEchantillonAsync(client);

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(1, creees);
        Assert.Equal(facture.Id, Assert.Single(await _base.Contexte.Factures.ToListAsync()).IdEchantillon);
    }

    [Fact]
    public async Task GenererManquantesAsync_AppelRepete_EstIdempotent()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(client);
        await AjouterEchantillonAsync(client);
        await CreerService().GenererManquantesAsync();

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(0, creees);
        Assert.Equal(2, await _base.Contexte.Factures.CountAsync());
    }

    [Fact]
    public async Task GenererManquantesAsync_NouvelEchantillonApresUneGeneration_NeFactureQueLeNouveau()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(client);
        await CreerService().GenererManquantesAsync();
        var nouveau = await AjouterEchantillonAsync(client);

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(1, creees);
        var factures = await _base.Contexte.Factures.AsNoTracking().OrderBy(facture => facture.NumeroFacture).ToListAsync();
        Assert.Equal([1, 2], factures.Select(facture => facture.NumeroFacture).ToArray());
        Assert.Equal(nouveau.Id, factures[1].IdEchantillon);
    }

    [Fact]
    public async Task GenererManquantesAsync_PremiereGeneration_NumeroteDepuisUnSelonLOrdreDePrelevement()
    {
        var client = await AjouterClientAsync("Acier SA");
        var recent = await AjouterEchantillonAsync(client, datePrelevement: BaseDonneesTest.DateReference.UtcDateTime.AddDays(-1));
        var ancien = await AjouterEchantillonAsync(client, datePrelevement: BaseDonneesTest.DateReference.UtcDateTime.AddDays(-5));
        var milieu = await AjouterEchantillonAsync(client, datePrelevement: BaseDonneesTest.DateReference.UtcDateTime.AddDays(-3));

        await CreerService().GenererManquantesAsync();

        var factures = await _base.Contexte.Factures.AsNoTracking().OrderBy(facture => facture.NumeroFacture).ToListAsync();
        Assert.Equal([1, 2, 3], factures.Select(facture => facture.NumeroFacture).ToArray());
        Assert.Equal([ancien.Id, milieu.Id, recent.Id], factures.Select(facture => facture.IdEchantillon).ToArray());
    }

    [Fact]
    public async Task GenererManquantesAsync_MemeDatePrelevement_DepartageParIdentifiantEchantillon()
    {
        var client = await AjouterClientAsync("Acier SA");
        var datePrelevement = BaseDonneesTest.DateReference.UtcDateTime.AddDays(-2);
        var premier = await AjouterEchantillonAsync(client, datePrelevement: datePrelevement);
        var second = await AjouterEchantillonAsync(client, datePrelevement: datePrelevement);

        await CreerService().GenererManquantesAsync();

        var factures = await _base.Contexte.Factures.AsNoTracking().OrderBy(facture => facture.NumeroFacture).ToListAsync();
        Assert.Equal([premier.Id, second.Id], factures.Select(facture => facture.IdEchantillon).ToArray());
    }

    [Fact]
    public async Task GenererManquantesAsync_NumeroMaximalExistant_ContinueLaSequenceSansTrou()
    {
        var client = await AjouterClientAsync("Acier SA");
        var deja = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(deja, client, numero: 41);
        await AjouterEchantillonAsync(client);
        await AjouterEchantillonAsync(client);

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(2, creees);
        var numeros = await _base.Contexte.Factures.AsNoTracking().OrderBy(facture => facture.NumeroFacture).Select(facture => facture.NumeroFacture).ToListAsync();
        Assert.Equal([41, 42, 43], numeros);
    }

    [Fact]
    public async Task GenererManquantesAsync_EchantillonAvecResultatValide_UtiliseLaDateDeValidation()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        _base.Contexte.ResultatsAgronomie.Add(new ResultatAgronomie
        {
            IdEchantillon = echantillon.Id,
            TypeSupport = "Sol",
            DateValidation = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc),
        });
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();

        await CreerService().GenererManquantesAsync();

        var facture = await _base.Contexte.Factures.AsNoTracking().SingleAsync();
        Assert.Equal(new DateOnly(2026, 9, 14), facture.DateFacture);
        Assert.Equal(new DateOnly(2026, 10, 14), facture.DateEcheance);
    }

    [Fact]
    public async Task GenererManquantesAsync_FactureEmiseDontLEcheanceEstDepassee_PasseEnRetard()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(echantillon, client, numero: 1, echeance: new DateOnly(2026, 9, 14));

        var creees = await CreerService().GenererManquantesAsync();

        Assert.Equal(0, creees);
        Assert.Equal(StatutsFacture.EnRetard, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_EcheanceAujourdhui_ResteEmise()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(echantillon, client, numero: 1, echeance: new DateOnly(2026, 9, 15));

        await CreerService().GenererManquantesAsync();

        Assert.Equal(StatutsFacture.Emise, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_HorlogeAvancee_BasculeEnRetardUneFactureEmiseExistante()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(echantillon, client, numero: 1, echeance: new DateOnly(2026, 9, 20));
        await CreerService().GenererManquantesAsync();
        Assert.Equal(StatutsFacture.Emise, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);

        _base.Horloge.Advance(TimeSpan.FromDays(6));
        await CreerService().GenererManquantesAsync();

        Assert.Equal(StatutsFacture.EnRetard, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_FacturePayeeEcheanceDepassee_RestePayee()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(echantillon, client, numero: 1, echeance: new DateOnly(2026, 8, 1), statut: StatutsFacture.Payee);

        await CreerService().GenererManquantesAsync();

        Assert.Equal(StatutsFacture.Payee, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_FactureSansEcheance_NEstPasPasseeEnRetard()
    {
        var client = await AjouterClientAsync("Acier SA");
        var echantillon = await AjouterEchantillonAsync(client);
        await AjouterFactureAsync(echantillon, client, numero: 1, echeance: null);

        await CreerService().GenererManquantesAsync();

        Assert.Equal(StatutsFacture.Emise, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_NouvelleFactureDejaEnRetard_EstEnregistreeEnRetard()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(client, datePrelevement: new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));

        await CreerService().GenererManquantesAsync();

        Assert.Equal(StatutsFacture.EnRetard, (await _base.Contexte.Factures.AsNoTracking().SingleAsync()).StatutFacture);
    }

    [Fact]
    public async Task GenererManquantesAsync_Creation_RenseigneLesChampsDAudit()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(client);

        await CreerService().GenererManquantesAsync();

        var facture = await _base.Contexte.Factures.AsNoTracking().SingleAsync();
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, facture.CreePar);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, facture.DateCreation);
    }

    private ServiceGenerationFactures CreerService()
        => new(
            _base.Contexte,
            new CalculateurFacture(Options.Create(new OptionsFacturation()), _base.Horloge),
            _base.Horloge);

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale, Ville = "Lyon" };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return client;
    }

    private async Task<Echantillon> AjouterEchantillonAsync(Client? client, string filiere = "Grandes cultures", DateTime? datePrelevement = null)
    {
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = $"ECH-{++_sequence:D5}",
            DatePrelevement = datePrelevement ?? BaseDonneesTest.DateReference.UtcDateTime.AddDays(-2),
            Filiere = filiere,
            IdClient = client?.Id,
        };
        _base.Contexte.Echantillons.Add(echantillon);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return echantillon;
    }

    private async Task AjouterFactureAsync(
        Echantillon echantillon,
        Client client,
        int numero,
        DateOnly? echeance = null,
        string statut = StatutsFacture.Emise)
    {
        _base.Contexte.Factures.Add(new Facture
        {
            IdEchantillon = echantillon.Id,
            IdClient = client.Id,
            NumeroFacture = numero,
            DateFacture = new DateOnly(2026, 8, 1),
            DateEcheance = echeance,
            NomClient = client.RaisonSociale,
            MontantHorsTaxe = 45m,
            TauxTva = 20m,
            MontantTva = 9m,
            MontantToutesTaxesComprises = 54m,
            Devise = "EUR",
            StatutFacture = statut,
        });
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
    }
}
