using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.UnitTests.Outils;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceRechercheFacturesTests : IDisposable
{
    private static readonly DateOnly _dateBase = new(2026, 9, 10);

    private readonly BaseDonneesTest _base = new();
    private int _sequence;

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task RechercherAsync_SansCritere_RetourneToutesLesFacturesAvecLeTotal()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client);
        await AjouterFactureAsync(client);
        await AjouterFactureAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures());

        Assert.Equal(3, resultat.NombreTotal);
        Assert.Equal(3, resultat.Elements.Count);
        Assert.Equal(1, resultat.NumeroPage);
        Assert.Equal(CriteresFactures.TaillePageParDefaut, resultat.TaillePage);
    }

    [Fact]
    public async Task RechercherAsync_BaseVide_RetourneUnePageVide()
    {
        var resultat = await CreerService().RechercherAsync(new CriteresFactures());

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_Facture_ProjetteLesValeursDeLaFactureEtDeLEchantillon()
    {
        var client = await AjouterClientAsync("Acier SA");
        var facture = await AjouterFactureAsync(client, filiere: "Viticulture", remise: 10m, commentaire: "Urgent");

        var resultat = await CreerService().RechercherAsync(new CriteresFactures());

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(facture.Id, ligne.IdFacture);
        Assert.Equal(facture.IdEchantillon, ligne.IdEchantillon);
        Assert.StartsWith("ECH-", ligne.CodeBarresAnonyme, StringComparison.Ordinal);
        Assert.Equal("Viticulture", ligne.Filiere);
        Assert.Equal(facture.NumeroFacture, ligne.NumeroFacture);
        Assert.Equal(_dateBase, ligne.DateFacture);
        Assert.Equal(_dateBase.AddDays(30), ligne.DateEcheance);
        Assert.Equal(client.Id, ligne.CodeClient);
        Assert.Equal("Acier SA", ligne.NomClient);
        Assert.Equal(45m, ligne.MontantHorsTaxe);
        Assert.Equal(20m, ligne.TauxTva);
        Assert.Equal(9m, ligne.MontantTva);
        Assert.Equal(54m, ligne.MontantToutesTaxesComprises);
        Assert.Equal(10m, ligne.Remise);
        Assert.Equal("EUR", ligne.Devise);
        Assert.Equal(StatutsFacture.Emise, ligne.StatutFacture);
        Assert.Equal("Urgent", ligne.Commentaire);
    }

    [Fact]
    public async Task RechercherAsync_FiltreDateDebut_InclutLaBorneEtExclutLesFacturesAnterieures()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-1));
        await AjouterFactureAsync(client, dateFacture: _dateBase);
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(1));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { DateFactureDebut = _dateBase });

        Assert.Equal([_dateBase.AddDays(1), _dateBase], resultat.Elements.Select(ligne => ligne.DateFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_FiltreDateFin_InclutLaBorneEtExclutLesFacturesPosterieures()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-1));
        await AjouterFactureAsync(client, dateFacture: _dateBase);
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(1));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { DateFactureFin = _dateBase });

        Assert.Equal([_dateBase, _dateBase.AddDays(-1)], resultat.Elements.Select(ligne => ligne.DateFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_PeriodeReduiteAUnJour_RetourneUniquementCeJour()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-1));
        await AjouterFactureAsync(client, dateFacture: _dateBase);
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(1));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { DateFactureDebut = _dateBase, DateFactureFin = _dateBase });

        Assert.Equal(_dateBase, Assert.Single(resultat.Elements).DateFacture);
    }

    [Fact]
    public async Task RechercherAsync_PeriodeInversee_NeRetourneRien()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"), dateFacture: _dateBase);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures
        {
            DateFactureDebut = _dateBase.AddDays(1),
            DateFactureFin = _dateBase.AddDays(-1),
        });

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_FiltreClient_RetourneUniquementLesFacturesDeCeClient()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        await AjouterFactureAsync(acier);
        await AjouterFactureAsync(acier);
        await AjouterFactureAsync(zinc);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { IdClient = acier.Id });

        Assert.Equal(2, resultat.NombreTotal);
        Assert.All(resultat.Elements, ligne => Assert.Equal(acier.Id, ligne.CodeClient));
    }

    [Fact]
    public async Task RechercherAsync_FiltreClientInexistant_NeRetourneRien()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { IdClient = 9_999 });

        Assert.Empty(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_FiltreStatut_RetourneUniquementCeStatut()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, statut: StatutsFacture.Emise);
        await AjouterFactureAsync(client, statut: StatutsFacture.EnRetard);
        await AjouterFactureAsync(client, statut: StatutsFacture.Payee);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { StatutFacture = StatutsFacture.EnRetard });

        Assert.Equal(StatutsFacture.EnRetard, Assert.Single(resultat.Elements).StatutFacture);
    }

    [Fact]
    public async Task RechercherAsync_StatutAvecEspaces_EstNettoyeAvantComparaison()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"), statut: StatutsFacture.Payee);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { StatutFacture = "  Payee " });

        Assert.Single(resultat.Elements);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RechercherAsync_StatutVide_NeFiltrePas(string? statut)
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, statut: StatutsFacture.Emise);
        await AjouterFactureAsync(client, statut: StatutsFacture.Payee);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { StatutFacture = statut });

        Assert.Equal(2, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_FiltresCombines_AppliquentTousLesCriteres()
    {
        var acier = await AjouterClientAsync("Acier SA");
        var zinc = await AjouterClientAsync("Zinc SARL");
        var attendue = await AjouterFactureAsync(acier, dateFacture: _dateBase, statut: StatutsFacture.EnRetard);
        await AjouterFactureAsync(acier, dateFacture: _dateBase, statut: StatutsFacture.Emise);
        await AjouterFactureAsync(zinc, dateFacture: _dateBase, statut: StatutsFacture.EnRetard);
        await AjouterFactureAsync(acier, dateFacture: _dateBase.AddDays(-20), statut: StatutsFacture.EnRetard);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures
        {
            DateFactureDebut = _dateBase.AddDays(-5),
            IdClient = acier.Id,
            StatutFacture = StatutsFacture.EnRetard,
        });

        Assert.Equal(attendue.Id, Assert.Single(resultat.Elements).IdFacture);
    }

    [Fact]
    public async Task RechercherAsync_TriParDefaut_DateDeFactureDecroissante()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-3));
        await AjouterFactureAsync(client, dateFacture: _dateBase);
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-1));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures());

        Assert.Equal(
            [_dateBase, _dateBase.AddDays(-1), _dateBase.AddDays(-3)],
            resultat.Elements.Select(ligne => ligne.DateFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriDateAscendant_DateCroissante()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateFacture: _dateBase);
        await AjouterFactureAsync(client, dateFacture: _dateBase.AddDays(-3));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { TriDescendant = false });

        Assert.Equal([_dateBase.AddDays(-3), _dateBase], resultat.Elements.Select(ligne => ligne.DateFacture).ToArray());
    }

    [Theory]
    [InlineData(false, new[] { 1, 2, 3 })]
    [InlineData(true, new[] { 3, 2, 1 })]
    public async Task RechercherAsync_TriParNumero_OrdonneSelonLeNumeroDeFacture(bool descendant, int[] attendu)
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, numero: 2);
        await AjouterFactureAsync(client, numero: 3);
        await AjouterFactureAsync(client, numero: 1);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { Tri = ChampTriFactures.NumeroFacture, TriDescendant = descendant });

        Assert.Equal(attendu, resultat.Elements.Select(ligne => ligne.NumeroFacture).ToArray());
    }

    [Theory]
    [InlineData(false, new[] { "Acier SA", "Moteur SA", "Zinc SARL" })]
    [InlineData(true, new[] { "Zinc SARL", "Moteur SA", "Acier SA" })]
    public async Task RechercherAsync_TriParClient_OrdonneSelonLeNomFige(bool descendant, string[] attendu)
    {
        await AjouterFactureAsync(await AjouterClientAsync("Moteur SA"));
        await AjouterFactureAsync(await AjouterClientAsync("Zinc SARL"));
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { Tri = ChampTriFactures.Client, TriDescendant = descendant });

        Assert.Equal(attendu, resultat.Elements.Select(ligne => ligne.NomClient).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriParEcheance_OrdonneSelonLEcheance()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, dateEcheance: _dateBase.AddDays(10));
        await AjouterFactureAsync(client, dateEcheance: _dateBase.AddDays(2));
        await AjouterFactureAsync(client, dateEcheance: _dateBase.AddDays(20));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { Tri = ChampTriFactures.DateEcheance, TriDescendant = false });

        Assert.Equal(
            [_dateBase.AddDays(2), _dateBase.AddDays(10), _dateBase.AddDays(20)],
            resultat.Elements.Select(ligne => ligne.DateEcheance).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriParMontantDescendant_OrdonneSelonLeMontantTtc()
    {
        // Le tri d'un decimal sur SQLite passe par une collation qui analyse le texte avec la culture courante.
        using var culture = new CultureScope();
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, montantHorsTaxe: 45m);
        await AjouterFactureAsync(client, montantHorsTaxe: 60m);
        await AjouterFactureAsync(client, montantHorsTaxe: 38m);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures
        {
            Tri = ChampTriFactures.MontantToutesTaxesComprises,
            TriDescendant = true,
        });

        Assert.Equal([72m, 54m, 45.60m], resultat.Elements.Select(ligne => ligne.MontantToutesTaxesComprises).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_TriParStatutAscendant_OrdonneSelonLeStatut()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client, statut: StatutsFacture.Payee);
        await AjouterFactureAsync(client, statut: StatutsFacture.Emise);
        await AjouterFactureAsync(client, statut: StatutsFacture.EnRetard);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { Tri = ChampTriFactures.StatutFacture, TriDescendant = false });

        Assert.Equal(
            [StatutsFacture.Emise, StatutsFacture.EnRetard, StatutsFacture.Payee],
            resultat.Elements.Select(ligne => ligne.StatutFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_ValeursDeTriIdentiquesAscendant_DepartageParIdentifiantCroissant()
    {
        var client = await AjouterClientAsync("Acier SA");
        var premiere = await AjouterFactureAsync(client);
        var seconde = await AjouterFactureAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { TriDescendant = false });

        Assert.Equal([premiere.Id, seconde.Id], resultat.Elements.Select(ligne => ligne.IdFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_ValeursDeTriIdentiquesDescendant_DepartageParIdentifiantDecroissant()
    {
        var client = await AjouterClientAsync("Acier SA");
        var premiere = await AjouterFactureAsync(client);
        var seconde = await AjouterFactureAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { TriDescendant = true });

        Assert.Equal([seconde.Id, premiere.Id], resultat.Elements.Select(ligne => ligne.IdFacture).ToArray());
    }

    [Fact]
    public async Task RechercherAsync_DeuxiemePage_RetourneLesElementsSuivantsSansChevauchement()
    {
        var client = await AjouterClientAsync("Acier SA");
        for (var index = 0; index < 10; index++)
        {
            await AjouterFactureAsync(client);
        }

        var premiere = await CreerService().RechercherAsync(new CriteresFactures { TaillePage = 4 });
        var seconde = await CreerService().RechercherAsync(new CriteresFactures { NumeroPage = 2, TaillePage = 4 });

        Assert.Equal(4, seconde.Elements.Count);
        Assert.Equal(10, seconde.NombreTotal);
        Assert.Equal(3, seconde.NombrePages);
        Assert.Empty(seconde.Elements.Select(ligne => ligne.IdFacture).Intersect(premiere.Elements.Select(ligne => ligne.IdFacture)));
    }

    [Fact]
    public async Task RechercherAsync_DernierePagePartielle_RetourneLesElementsRestants()
    {
        var client = await AjouterClientAsync("Acier SA");
        for (var index = 0; index < 10; index++)
        {
            await AjouterFactureAsync(client);
        }

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { NumeroPage = 3, TaillePage = 4 });

        Assert.Equal(2, resultat.Elements.Count);
    }

    [Fact]
    public async Task RechercherAsync_TaillePageSuperieureAuMaximum_EstRameneeAuMaximum()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { TaillePage = 5_000 });

        Assert.Equal(CriteresFactures.TaillePageMaximum, resultat.TaillePage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task RechercherAsync_TaillePageNulleOuNegative_EstRameneeAUn(int taillePage)
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterFactureAsync(client);
        await AjouterFactureAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { TaillePage = taillePage });

        Assert.Equal(1, resultat.TaillePage);
        Assert.Single(resultat.Elements);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task RechercherAsync_NumeroPageInferieurAUn_RetourneLaPremierePage(int numeroPage)
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { NumeroPage = numeroPage });

        Assert.Equal(1, resultat.NumeroPage);
        Assert.Single(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_PageAuDelaDuDernier_RetourneUnePageVideAvecLeTotal()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures { NumeroPage = 50 });

        Assert.Empty(resultat.Elements);
        Assert.Equal(1, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_NumeroPageMaximalAvecGrandePage_NeDepassePasLesBornesEntieres()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures
        {
            NumeroPage = int.MaxValue,
            TaillePage = CriteresFactures.TaillePageMaximum,
        });

        Assert.Equal(int.MaxValue / CriteresFactures.TaillePageMaximum, resultat.NumeroPage);
        Assert.Empty(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_DatesSqlite_SontRestitueesSansDecalageNiHeure()
    {
        await AjouterFactureAsync(await AjouterClientAsync("Acier SA"), dateFacture: new DateOnly(2026, 1, 31));

        var resultat = await CreerService().RechercherAsync(new CriteresFactures
        {
            DateFactureDebut = new DateOnly(2026, 1, 31),
            DateFactureFin = new DateOnly(2026, 1, 31),
        });

        Assert.Equal(new DateOnly(2026, 1, 31), Assert.Single(resultat.Elements).DateFacture);
    }

    [Theory]
    [InlineData(10, 90, 100)]
    [InlineData(25, 75, 100)]
    public void MontantHorsTaxeAvantRemise_AvecRemise_ReconstitueLeMontantInitial(double remise, double montantRemise, double montantInitial)
    {
        var dto = CreerDto((decimal)montantRemise, (decimal)remise);

        Assert.Equal((decimal)montantInitial, dto.MontantHorsTaxeAvantRemise);
    }

    [Fact]
    public void MontantHorsTaxeAvantRemise_RemiseAbsente_RetourneLeMontantHorsTaxe()
    {
        var dto = CreerDto(45m, remise: null);

        Assert.Equal(45m, dto.MontantHorsTaxeAvantRemise);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void MontantHorsTaxeAvantRemise_RemiseNulleOuTotale_RetourneLeMontantHorsTaxe(double remise)
    {
        var dto = CreerDto(45m, (decimal)remise);

        Assert.Equal(45m, dto.MontantHorsTaxeAvantRemise);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly System.Globalization.CultureInfo _precedente = System.Globalization.CultureInfo.CurrentCulture;

        public CultureScope() => System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        public void Dispose() => System.Globalization.CultureInfo.CurrentCulture = _precedente;
    }

    private static FactureDto CreerDto(decimal montantHorsTaxe, decimal? remise)
        => new(1, 1, "ECH-00001", "Elevage", 1, _dateBase, null, 1, "Acier SA", null, null, null, null, null,
            montantHorsTaxe, 20m, 0m, montantHorsTaxe, remise, "EUR", StatutsFacture.Emise, null, null);

    private ServiceRechercheFactures CreerService() => new(_base.Contexte);

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return client;
    }

    private async Task<Facture> AjouterFactureAsync(
        Client client,
        string filiere = "Grandes cultures",
        DateOnly? dateFacture = null,
        DateOnly? dateEcheance = null,
        string statut = StatutsFacture.Emise,
        int? numero = null,
        decimal montantHorsTaxe = 45m,
        decimal? remise = null,
        string? commentaire = null)
    {
        var sequence = ++_sequence;
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = $"ECH-{sequence:D5}",
            DatePrelevement = BaseDonneesTest.DateReference.UtcDateTime,
            Filiere = filiere,
            IdClient = client.Id,
        };
        var date = dateFacture ?? _dateBase;
        var montantTva = Math.Round(montantHorsTaxe * 0.2m, 2, MidpointRounding.AwayFromZero);
        var facture = new Facture
        {
            Echantillon = echantillon,
            IdClient = client.Id,
            NumeroFacture = numero ?? sequence,
            DateFacture = date,
            DateEcheance = dateEcheance ?? date.AddDays(30),
            NomClient = client.RaisonSociale,
            MontantHorsTaxe = montantHorsTaxe,
            TauxTva = 20m,
            MontantTva = montantTva,
            MontantToutesTaxesComprises = montantHorsTaxe + montantTva,
            Remise = remise,
            Devise = "EUR",
            StatutFacture = statut,
            Commentaire = commentaire,
        };
        _base.Contexte.Factures.Add(facture);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return facture;
    }
}
