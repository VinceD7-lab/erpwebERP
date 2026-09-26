using erpWeb.Core.Audit;
using erpWeb.UnitTests.Outils;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceRechercheJournalAuditTests : IDisposable
{
    private static readonly DateTime _maintenant = BaseDonneesTest.DateReference.UtcDateTime;

    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task RechercherAsync_SansCritere_RetourneLaPremierePageTrieeParDateDescendante()
    {
        await AjouterEntreesAsync(5);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit());

        Assert.Equal(1, resultat.NumeroPage);
        Assert.Equal(5, resultat.NombreTotal);
        Assert.Equal(_maintenant, resultat.Elements[0].Date);
        Assert.True(resultat.Elements[0].Date > resultat.Elements[^1].Date);
    }

    [Fact]
    public async Task RechercherAsync_DeuxiemePage_RetourneLesElementsSuivantsEtLeTotalComplet()
    {
        await AjouterEntreesAsync(10);

        var premiere = await CreerService().RechercherAsync(new CriteresJournalAudit { TaillePage = 4 });
        var seconde = await CreerService().RechercherAsync(new CriteresJournalAudit { NumeroPage = 2, TaillePage = 4 });

        Assert.Equal(4, seconde.Elements.Count);
        Assert.Equal(10, seconde.NombreTotal);
        Assert.Equal(3, seconde.NombrePages);
        Assert.Empty(seconde.Elements.Select(entree => entree.Id).Intersect(premiere.Elements.Select(entree => entree.Id)));
    }

    [Fact]
    public async Task RechercherAsync_TaillePageSuperieureAuMaximum_EstRameneeAuMaximum()
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { TaillePage = 5_000 });

        Assert.Equal(CriteresJournalAudit.TaillePageMaximum, resultat.TaillePage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task RechercherAsync_TaillePageNulleOuNegative_EstRameneeAUn(int taillePage)
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { TaillePage = taillePage });

        Assert.Equal(1, resultat.TaillePage);
        Assert.Single(resultat.Elements);
    }

    [Fact]
    public async Task RechercherAsync_NumeroPageInferieurAUn_RetourneLaPremierePage()
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { NumeroPage = -2 });

        Assert.Equal(1, resultat.NumeroPage);
        Assert.Equal(3, resultat.Elements.Count);
    }

    [Fact]
    public async Task RechercherAsync_PageAuDelaDuDernier_RetourneUnePageVideAvecLeTotal()
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { NumeroPage = 50 });

        Assert.Empty(resultat.Elements);
        Assert.Equal(3, resultat.NombreTotal);
    }

    [Theory]
    [InlineData(int.MaxValue, CriteresJournalAudit.TaillePageMaximum)]
    [InlineData(10_737_420, CriteresJournalAudit.TaillePageMaximum)]
    [InlineData(int.MaxValue, 1)]
    public async Task RechercherAsync_NumeroPageTresGrand_RetourneUnePageVideSansDebordement(int numeroPage, int taillePage)
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { NumeroPage = numeroPage, TaillePage = taillePage });

        // Avant la borne, le décalage débordait en négatif et SQLite renvoyait la première page.
        Assert.Empty(resultat.Elements);
        Assert.Equal(3, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_SansResultat_RetourneUneSeulePage()
    {
        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit());

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
        Assert.Equal(1, resultat.NombrePages);
    }

    [Fact]
    public async Task RechercherAsync_FiltreSurAction_NeRetourneQueLesEntreesCorrespondantes()
    {
        _base.Contexte.JournalAudit.AddRange(
            CreerEntree(_maintenant, action: ActionAudit.Creation),
            CreerEntree(_maintenant.AddMinutes(-1), action: ActionAudit.Suppression),
            CreerEntree(_maintenant.AddMinutes(-2), action: ActionAudit.Suppression));
        await _base.Contexte.SaveChangesAsync();

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { Action = ActionAudit.Suppression });

        Assert.Equal(2, resultat.NombreTotal);
        Assert.All(resultat.Elements, entree => Assert.Equal(ActionAudit.Suppression, entree.Action));
    }

    [Fact]
    public async Task RechercherAsync_FiltreSurPeriode_FiltreSurLIntervalleSemiOuvert()
    {
        var debut = _maintenant.AddDays(-1);
        _base.Contexte.JournalAudit.AddRange(
            CreerEntree(debut.AddSeconds(-1)),
            CreerEntree(debut),
            CreerEntree(_maintenant.AddSeconds(-1)),
            CreerEntree(_maintenant));
        await _base.Contexte.SaveChangesAsync();

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit
        {
            DateDebut = new DateTimeOffset(debut, TimeSpan.Zero),
            DateFin = BaseDonneesTest.DateReference,
        });

        Assert.Equal(2, resultat.NombreTotal);
        Assert.DoesNotContain(resultat.Elements, entree => entree.Date == _maintenant);
        Assert.Contains(resultat.Elements, entree => entree.Date == debut);
    }

    [Theory]
    [InlineData("dupont@erpweb.local")]
    [InlineData("Facture")]
    [InlineData("4242")]
    public async Task RechercherAsync_RechercheLibre_TrouveParUtilisateurTypeEntiteOuIdentifiant(string recherche)
    {
        _base.Contexte.JournalAudit.AddRange(
            CreerEntree(_maintenant, typeEntite: "Facture", idEntite: "4242", utilisateur: "dupont@erpweb.local"),
            CreerEntree(_maintenant.AddMinutes(-1), typeEntite: "Parametre", idEntite: "7", utilisateur: "martin@erpweb.local"));
        await _base.Contexte.SaveChangesAsync();

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { Recherche = recherche });

        Assert.Equal(1, resultat.NombreTotal);
        Assert.Equal("Facture", resultat.Elements[0].TypeEntite);
    }

    [Fact]
    public async Task RechercherAsync_TriParUtilisateurAscendant_OrdonneLesEntrees()
    {
        _base.Contexte.JournalAudit.AddRange(
            CreerEntree(_maintenant, utilisateur: "zoe@erpweb.local"),
            CreerEntree(_maintenant.AddMinutes(-1), utilisateur: "alice@erpweb.local"),
            CreerEntree(_maintenant.AddMinutes(-2), utilisateur: "marc@erpweb.local"));
        await _base.Contexte.SaveChangesAsync();

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit
        {
            Tri = ChampTriJournalAudit.Utilisateur,
            TriDescendant = false,
        });

        Assert.Equal(
            ["alice@erpweb.local", "marc@erpweb.local", "zoe@erpweb.local"],
            resultat.Elements.Select(entree => entree.Utilisateur));
    }

    [Fact]
    public async Task RechercherAsync_DatesIdentiques_DepartageParIdentifiantDescendant()
    {
        await AjouterEntreesAsync(4, dateIdentique: true);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit());

        Assert.Equal(
            resultat.Elements.Select(entree => entree.Id).OrderByDescending(identifiant => identifiant),
            resultat.Elements.Select(entree => entree.Id));
    }

    [Fact]
    public async Task RechercherAsync_Entrees_RetourneDesDatesEnUtc()
    {
        await AjouterEntreesAsync(1);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit());

        Assert.Equal(DateTimeKind.Utc, resultat.Elements[0].Date.Kind);
    }

    [Fact]
    public async Task RechercherAsync_RechercheSansCorrespondance_RetourneUnePageVide()
    {
        await AjouterEntreesAsync(3);

        var resultat = await CreerService().RechercherAsync(new CriteresJournalAudit { Recherche = "inexistant" });

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
        Assert.Equal(1, resultat.NombrePages);
    }

    private ServiceRechercheJournalAudit CreerService() => new(_base.Contexte, BaseDonneesTest.CreerMapper());

    private async Task AjouterEntreesAsync(int nombre, bool dateIdentique = false)
    {
        for (var decalage = 0; decalage < nombre; decalage++)
        {
            _base.Contexte.JournalAudit.Add(CreerEntree(dateIdentique ? _maintenant : _maintenant.AddMinutes(-decalage)));
        }

        await _base.Contexte.SaveChangesAsync();
    }

    private static EntreeJournalAudit CreerEntree(
        DateTime date,
        ActionAudit action = ActionAudit.Modification,
        string typeEntite = "Parametre",
        string idEntite = "1",
        string? utilisateur = null) => new()
        {
            TypeEntite = typeEntite,
            IdEntite = idEntite,
            Action = action,
            Utilisateur = utilisateur ?? BaseDonneesTest.NomUtilisateurTest,
            Date = date,
        };
}
