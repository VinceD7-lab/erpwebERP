using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Tournees;
using erpWeb.UnitTests.Outils;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceRechercheResultatsEchantillonsTests : IDisposable
{
    private static readonly DateOnly _dateBase = new(2026, 9, 10);

    private readonly BaseDonneesTest _base = new();
    private int _sequence;

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task RechercherAsync_SansCritere_RetourneTousLesEchantillonsAvecLeTotal()
    {
        var client = await AjouterClientAsync("Acier SA");
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, client);
        await AjouterEchantillonAsync(tournee, client);
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons());

        Assert.Equal(3, resultat.NombreTotal);
        Assert.Equal(3, resultat.Elements.Count);
        Assert.Equal(1, resultat.NumeroPage);
        Assert.Equal(CriteresResultatsEchantillons.TaillePageParDefaut, resultat.TaillePage);
    }

    [Fact]
    public async Task RechercherAsync_BaseVide_RetourneUnePageVide()
    {
        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons());

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_EchantillonSansTourneeNiClientNiResultat_ExposeDesValeursNulles()
    {
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons());

        var ligne = Assert.Single(resultat.Elements);
        Assert.Null(ligne.DateTournee);
        Assert.Null(ligne.IdClient);
        Assert.Null(ligne.RaisonSocialeClient);
        Assert.Null(ligne.TypeSupport);
        Assert.Null(ligne.PhSol);
        Assert.Null(ligne.DateValidation);
    }

    [Fact]
    public async Task RechercherAsync_EchantillonAvecResultat_ProjetteLesValeursDeLaTourneeDuClientEtDuResultat()
    {
        var client = await AjouterClientAsync("Acier SA");
        var tournee = await AjouterTourneeAsync(_dateBase);
        var dateValidation = new DateTime(2026, 9, 12, 9, 0, 0, DateTimeKind.Utc);
        var echantillon = await AjouterEchantillonAsync(tournee, client, statut: StatutsAnalyse.Termine, temperature: 4.5m);
        _base.Contexte.ResultatsAgronomie.Add(new ResultatAgronomie
        {
            IdEchantillon = echantillon.Id,
            TypeSupport = "Sol",
            PhSol = 6.5m,
            MatiereOrganique = 2.25m,
            PhosphoreP2O5 = 120m,
            PotassiumK2O = 200m,
            ReliquatAzoteN = 40m,
            ValeurUclFourrage = 0.9m,
            DateValidation = dateValidation,
        });
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons());

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(echantillon.Id, ligne.IdEchantillon);
        Assert.Equal(_dateBase, ligne.DateTournee);
        Assert.Equal(client.Id, ligne.IdClient);
        Assert.Equal("Acier SA", ligne.RaisonSocialeClient);
        Assert.Equal(echantillon.CodeBarresAnonyme, ligne.CodeBarresAnonyme);
        Assert.Equal(StatutsAnalyse.Termine, ligne.StatutAnalyse);
        Assert.Equal(4.5m, ligne.TemperatureReception);
        Assert.Equal("Sol", ligne.TypeSupport);
        Assert.Equal(6.5m, ligne.PhSol);
        Assert.Equal(2.25m, ligne.MatiereOrganique);
        Assert.Equal(120m, ligne.PhosphoreP2O5);
        Assert.Equal(200m, ligne.PotassiumK2O);
        Assert.Equal(40m, ligne.ReliquatAzoteN);
        Assert.Equal(0.9m, ligne.ValeurUclFourrage);
        Assert.Equal(dateValidation, ligne.DateValidation);
    }

    [Fact]
    public async Task RechercherAsync_FiltreDateDebut_InclutLaBorneEtExclutLesTourneesAnterieures()
    {
        var avant = await AjouterTourneeAsync(_dateBase.AddDays(-1));
        var pile = await AjouterTourneeAsync(_dateBase);
        var apres = await AjouterTourneeAsync(_dateBase.AddDays(1));
        await AjouterEchantillonAsync(avant, null);
        await AjouterEchantillonAsync(pile, null);
        await AjouterEchantillonAsync(apres, null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { DateTourneeDebut = _dateBase });

        Assert.Equal([_dateBase.AddDays(1), _dateBase], resultat.Elements.Select(ligne => ligne.DateTournee).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_FiltreDateFin_InclutLaBorneEtExclutLesTourneesPosterieures()
    {
        var avant = await AjouterTourneeAsync(_dateBase.AddDays(-1));
        var pile = await AjouterTourneeAsync(_dateBase);
        var apres = await AjouterTourneeAsync(_dateBase.AddDays(1));
        await AjouterEchantillonAsync(avant, null);
        await AjouterEchantillonAsync(pile, null);
        await AjouterEchantillonAsync(apres, null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { DateTourneeFin = _dateBase });

        Assert.Equal([_dateBase, _dateBase.AddDays(-1)], resultat.Elements.Select(ligne => ligne.DateTournee).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_PeriodeReduiteAUnJour_RetourneUniquementCeJour()
    {
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase.AddDays(-1)), null);
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase), null);
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase.AddDays(1)), null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            DateTourneeDebut = _dateBase,
            DateTourneeFin = _dateBase,
        });

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(_dateBase, ligne.DateTournee);
    }

    [Fact]
    public async Task RechercherAsync_PeriodeInversee_NeRetourneRien()
    {
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase), null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            DateTourneeDebut = _dateBase.AddDays(1),
            DateTourneeFin = _dateBase.AddDays(-1),
        });

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_FiltreSurPeriode_ExclutLesEchantillonsSansTournee()
    {
        await AjouterEchantillonAsync(tournee: null, client: null);
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase), null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { DateTourneeDebut = _dateBase.AddDays(-30) });

        Assert.Single(resultat.Elements);
        Assert.All(resultat.Elements, ligne => Assert.NotNull(ligne.DateTournee));
    }

    [Fact]
    public async Task RechercherAsync_FiltreClient_RetourneUniquementLesEchantillonsDeCeClient()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, acier);
        await AjouterEchantillonAsync(tournee, acier);
        await AjouterEchantillonAsync(tournee, zinc);
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { IdClient = acier.Id });

        Assert.Equal(2, resultat.NombreTotal);
        Assert.All(resultat.Elements, ligne => Assert.Equal(acier.Id, ligne.IdClient));
    }

    [Fact]
    public async Task RechercherAsync_FiltreClientInexistant_NeRetourneRien()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase), client);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { IdClient = 9_999 });

        Assert.Empty(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_FiltreStatut_RetourneUniquementCeStatut()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Recu);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.EnCours);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Termine);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { StatutAnalyse = StatutsAnalyse.EnCours });

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(StatutsAnalyse.EnCours, ligne.StatutAnalyse);
    }

    [Fact]
    public async Task RechercherAsync_StatutAvecEspaces_EstNettoyeAvantComparaison()
    {
        await AjouterEchantillonAsync(await AjouterTourneeAsync(_dateBase), null, statut: StatutsAnalyse.Termine);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { StatutAnalyse = "  Termine " });

        Assert.Single(resultat.Elements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RechercherAsync_StatutVide_NeFiltrePas(string? statut)
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Recu);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Termine);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { StatutAnalyse = statut });

        Assert.Equal(2, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_FiltresCombines_AppliquentTousLesCriteres()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var tourneeRecente = await AjouterTourneeAsync(_dateBase);
        var tourneeAncienne = await AjouterTourneeAsync(_dateBase.AddDays(-20));
        var attendu = await AjouterEchantillonAsync(tourneeRecente, acier, statut: StatutsAnalyse.Termine);
        await AjouterEchantillonAsync(tourneeRecente, acier, statut: StatutsAnalyse.Recu);
        await AjouterEchantillonAsync(tourneeRecente, zinc, statut: StatutsAnalyse.Termine);
        await AjouterEchantillonAsync(tourneeAncienne, acier, statut: StatutsAnalyse.Termine);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            DateTourneeDebut = _dateBase.AddDays(-5),
            IdClient = acier.Id,
            StatutAnalyse = StatutsAnalyse.Termine,
        });

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(attendu.Id, ligne.IdEchantillon);
    }

    [Fact]
    public async Task RechercherAsync_TriParDefaut_DateDeTourneeDecroissantePuisClient()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var ancienne = await AjouterTourneeAsync(_dateBase.AddDays(-3));
        var recente = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(ancienne, acier);
        await AjouterEchantillonAsync(recente, zinc);
        await AjouterEchantillonAsync(recente, acier);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons());

        // Dans le tri par défaut descendant, le second critère (client) l'est aussi.
        Assert.Equal(
            [(_dateBase, "Zinc SARL"), (_dateBase, "Acier SA"), (_dateBase.AddDays(-3), "Acier SA")],
            resultat.Elements.Select(ligne => (ligne.DateTournee!.Value, ligne.RaisonSocialeClient!)).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriDateAscendant_DateCroissantePuisClientCroissant()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var ancienne = await AjouterTourneeAsync(_dateBase.AddDays(-3));
        var recente = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(recente, zinc);
        await AjouterEchantillonAsync(recente, acier);
        await AjouterEchantillonAsync(ancienne, zinc);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { TriDescendant = false });

        Assert.Equal(
            [(_dateBase.AddDays(-3), "Zinc SARL"), (_dateBase, "Acier SA"), (_dateBase, "Zinc SARL")],
            resultat.Elements.Select(ligne => (ligne.DateTournee!.Value, ligne.RaisonSocialeClient!)).ToArray());
    }

    [Theory]
    [InlineData(ChampTriResultatsEchantillons.Client, false, new[] { "Acier SA", "Moteur SA", "Zinc SARL" })]
    [InlineData(ChampTriResultatsEchantillons.Client, true, new[] { "Zinc SARL", "Moteur SA", "Acier SA" })]
    public async Task RechercherAsync_TriParClient_OrdonneSelonLaRaisonSociale(ChampTriResultatsEchantillons tri, bool descendant, string[] attendu)
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, await AjouterClientAsync("Moteur SA"));
        await AjouterEchantillonAsync(tournee, await AjouterClientAsync("Zinc SARL"));
        await AjouterEchantillonAsync(tournee, await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { Tri = tri, TriDescendant = descendant });

        Assert.Equal(attendu, resultat.Elements.Select(ligne => ligne.RaisonSocialeClient).ToArray());
    }

    [Theory]
    [InlineData(false, new[] { "ECH-A", "ECH-B", "ECH-C" })]
    [InlineData(true, new[] { "ECH-C", "ECH-B", "ECH-A" })]
    public async Task RechercherAsync_TriParCodeBarres_OrdonneSelonLeCode(bool descendant, string[] attendu)
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, null, codeBarres: "ECH-B");
        await AjouterEchantillonAsync(tournee, null, codeBarres: "ECH-C");
        await AjouterEchantillonAsync(tournee, null, codeBarres: "ECH-A");

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            Tri = ChampTriResultatsEchantillons.CodeBarres,
            TriDescendant = descendant,
        });

        Assert.Equal(attendu, resultat.Elements.Select(ligne => ligne.CodeBarresAnonyme).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriParFiliere_OrdonneSelonLaFiliere()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, null, filiere: "Maraichage");
        await AjouterEchantillonAsync(tournee, null, filiere: "Elevage");
        await AjouterEchantillonAsync(tournee, null, filiere: "Viticulture");

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            Tri = ChampTriResultatsEchantillons.Filiere,
            TriDescendant = false,
        });

        Assert.Equal(["Elevage", "Maraichage", "Viticulture"], resultat.Elements.Select(ligne => ligne.Filiere).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriParStatutDescendant_OrdonneSelonLeStatut()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.EnCours);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Termine);
        await AjouterEchantillonAsync(tournee, null, statut: StatutsAnalyse.Recu);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            Tri = ChampTriResultatsEchantillons.StatutAnalyse,
            TriDescendant = true,
        });

        Assert.Equal(
            [StatutsAnalyse.Termine, StatutsAnalyse.Recu, StatutsAnalyse.EnCours],
            resultat.Elements.Select(ligne => ligne.StatutAnalyse).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_ValeursDeTriIdentiques_DepartageParIdentifiantPourUnOrdreStable()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        var premier = await AjouterEchantillonAsync(tournee, null, filiere: "Elevage");
        var second = await AjouterEchantillonAsync(tournee, null, filiere: "Elevage");

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            Tri = ChampTriResultatsEchantillons.Filiere,
            TriDescendant = false,
        });

        Assert.Equal([premier.Id, second.Id], resultat.Elements.Select(ligne => ligne.IdEchantillon).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_DeuxiemePage_RetourneLesElementsSuivantsSansChevauchement()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        for (var index = 0; index < 10; index++)
        {
            await AjouterEchantillonAsync(tournee, null);
        }

        var premiere = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { TaillePage = 4 });
        var seconde = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { NumeroPage = 2, TaillePage = 4 });

        Assert.Equal(4, seconde.Elements.Count);
        Assert.Equal(10, seconde.NombreTotal);
        Assert.Equal(3, seconde.NombrePages);
        Assert.Empty(seconde.Elements.Select(ligne => ligne.IdEchantillon).Intersect(premiere.Elements.Select(ligne => ligne.IdEchantillon)));
    }

    [Fact]
    public async Task RechercherAsync_DernierePagePartielle_RetourneLesElementsRestants()
    {
        var tournee = await AjouterTourneeAsync(_dateBase);
        for (var index = 0; index < 10; index++)
        {
            await AjouterEchantillonAsync(tournee, null);
        }

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { NumeroPage = 3, TaillePage = 4 });

        Assert.Equal(2, resultat.Elements.Count);
    }

    [Fact]
    public async Task RechercherAsync_TaillePageSuperieureAuMaximum_EstRameneeAuMaximum()
    {
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { TaillePage = 5_000 });

        Assert.Equal(CriteresResultatsEchantillons.TaillePageMaximum, resultat.TaillePage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task RechercherAsync_TaillePageNulleOuNegative_EstRameneeAUn(int taillePage)
    {
        await AjouterEchantillonAsync(tournee: null, client: null);
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { TaillePage = taillePage });

        Assert.Equal(1, resultat.TaillePage);
        Assert.Single(resultat.Elements);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task RechercherAsync_NumeroPageInferieurAUn_RetourneLaPremierePage(int numeroPage)
    {
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { NumeroPage = numeroPage });

        Assert.Equal(1, resultat.NumeroPage);
        Assert.Single(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_PageAuDelaDuDernier_RetourneUnePageVideAvecLeTotal()
    {
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons { NumeroPage = 50 });

        Assert.Empty(resultat.Elements);
        Assert.Equal(1, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_NumeroPageMaximalAvecGrandePage_NeDepassePasLesBornesEntieres()
    {
        await AjouterEchantillonAsync(tournee: null, client: null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            NumeroPage = int.MaxValue,
            TaillePage = CriteresResultatsEchantillons.TaillePageMaximum,
        });

        Assert.Equal(int.MaxValue / CriteresResultatsEchantillons.TaillePageMaximum, resultat.NumeroPage);
        Assert.Empty(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_DatesDeTourneeSqlite_SontRestitueesSansDecalageNiHeure()
    {
        await AjouterEchantillonAsync(await AjouterTourneeAsync(new DateOnly(2026, 1, 31)), null);

        var resultat = await CreerService().RechercherAsync(new CriteresResultatsEchantillons
        {
            DateTourneeDebut = new DateOnly(2026, 1, 31),
            DateTourneeFin = new DateOnly(2026, 1, 31),
        });

        Assert.Equal(new DateOnly(2026, 1, 31), Assert.Single(resultat.Elements).DateTournee);
    }

    private ServiceRechercheResultatsEchantillons CreerService() => new(_base.Contexte);

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        return client;
    }

    private async Task<TourneeRamassage> AjouterTourneeAsync(DateOnly date)
    {
        var tournee = new TourneeRamassage { DateTournee = date, NomChauffeur = "Chauffeur Test", StatutTemperature = StatutsTemperature.Conforme };
        _base.Contexte.TourneesRamassage.Add(tournee);
        await _base.Contexte.SaveChangesAsync();
        return tournee;
    }

    private async Task<Echantillon> AjouterEchantillonAsync(
        TourneeRamassage? tournee,
        Client? client,
        string statut = StatutsAnalyse.Recu,
        string filiere = "Grandes cultures",
        string? codeBarres = null,
        decimal? temperature = null)
    {
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = codeBarres ?? $"ECH-{++_sequence:D5}",
            DatePrelevement = BaseDonneesTest.DateReference.UtcDateTime,
            Filiere = filiere,
            StatutAnalyse = statut,
            TemperatureReception = temperature,
            IdTournee = tournee?.Id,
            IdClient = client?.Id,
        };
        _base.Contexte.Echantillons.Add(echantillon);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return echantillon;
    }
}
