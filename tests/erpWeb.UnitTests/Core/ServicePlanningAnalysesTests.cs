using Bogus;
using erpWeb.Core.Clients;
using erpWeb.Core.PlanningAnalyses;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.UnitTests.Core;

public sealed class ServicePlanningAnalysesTests : IDisposable
{
    // Septembre 2026 : le 1er est un mardi, le 5 un samedi, le 6 un dimanche.
    private const int Annee = 2026;
    private const int Mois = 9;

    private readonly BaseDonneesTest _base = new();
    private readonly Faker _faker = new("fr");

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task ObtenirMoisAsync_MoisDemande_RetourneTousLesJoursAvecLIndicateurOuvre()
    {
        var planning = await CreerService().ObtenirMoisAsync(Annee, Mois);

        Assert.Equal(Annee, planning.Annee);
        Assert.Equal(Mois, planning.Mois);
        Assert.Equal(30, planning.Jours.Count);
        Assert.Equal(new DateOnly(2026, 9, 1), planning.Jours[0].Date);
        Assert.Equal(new DateOnly(2026, 9, 30), planning.Jours[^1].Date);
        Assert.True(planning.Jours.Single(jour => jour.Date.Day == 4).EstOuvre);
        Assert.False(planning.Jours.Single(jour => jour.Date.Day == 5).EstOuvre);
        Assert.False(planning.Jours.Single(jour => jour.Date.Day == 6).EstOuvre);
        Assert.True(planning.Jours.Single(jour => jour.Date.Day == 7).EstOuvre);
        Assert.Equal(22, planning.Jours.Count(jour => jour.EstOuvre));
    }

    [Fact]
    public async Task ObtenirMoisAsync_FevrierBissextile_Retourne29Jours()
    {
        var planning = await CreerService().ObtenirMoisAsync(2028, 2);

        Assert.Equal(29, planning.Jours.Count);
    }

    [Fact]
    public async Task ObtenirMoisAsync_AucuneSaisie_RetourneLeMoisCourantDeLHorloge()
    {
        var planning = await CreerService().ObtenirMoisAsync(null, null);

        Assert.Equal(2026, planning.Annee);
        Assert.Equal(9, planning.Mois);
    }

    [Fact]
    public async Task ObtenirMoisAsync_HorlogeAvancee_SuitLeMoisCourant()
    {
        _base.Horloge.Advance(TimeSpan.FromDays(30));

        var planning = await CreerService().ObtenirMoisAsync(null, null);

        Assert.Equal(2026, planning.Annee);
        Assert.Equal(10, planning.Mois);
    }

    [Theory]
    [InlineData(2026, 13)]
    [InlineData(2026, 0)]
    [InlineData(1999, 5)]
    [InlineData(2101, 5)]
    [InlineData(null, 5)]
    [InlineData(2026, null)]
    public async Task ObtenirMoisAsync_PeriodeInvalide_RetourneLeMoisCourant(int? annee, int? mois)
    {
        var planning = await CreerService().ObtenirMoisAsync(annee, mois);

        Assert.Equal(2026, planning.Annee);
        Assert.Equal(9, planning.Mois);
    }

    [Fact]
    public async Task ObtenirMoisAsync_PlusieursClients_RetourneLesClientsTriesParRaisonSociale()
    {
        await AjouterClientAsync("Zinc SARL");
        await AjouterClientAsync("Acier SA");

        var planning = await CreerService().ObtenirMoisAsync(Annee, Mois);

        Assert.Equal(["Acier SA", "Zinc SARL"], planning.Clients.Select(client => client.RaisonSociale));
    }

    [Fact]
    public async Task ObtenirMoisAsync_CasesEnregistrees_RetourneUniquementLesCasesDuMois()
    {
        var client = await AjouterClientAsync(_faker.Company.CompanyName());
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 8, 31), TypesAnalysePlanning.A);
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 1), TypesAnalysePlanning.B);
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 30), TypesAnalysePlanning.C);
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 10, 1), TypesAnalysePlanning.D);

        var planning = await CreerService().ObtenirMoisAsync(Annee, Mois);

        Assert.Equal(2, planning.Cases.Count);
        Assert.Equal(TypesAnalysePlanning.B, planning.Cases.Single(caseplanning => caseplanning.Date.Day == 1).TypeAnalyse);
        Assert.Equal(TypesAnalysePlanning.C, planning.Cases.Single(caseplanning => caseplanning.Date.Day == 30).TypeAnalyse);
        Assert.All(planning.Cases, caseplanning => Assert.Equal(client.Id, caseplanning.IdClient));
    }

    [Fact]
    public async Task SauvegarderMoisAsync_NouvellesCases_CreeLesLignesEtRenseigneLAudit()
    {
        var client = await AjouterClientAsync("Acier SA");
        var sauvegarde = CreerSauvegarde(
            Case(client.Id, 1, TypesAnalysePlanning.A),
            Case(client.Id, 2, TypesAnalysePlanning.D));

        var resultat = await CreerService().SauvegarderMoisAsync(sauvegarde);

        Assert.True(resultat.Reussi);
        var lignes = await _base.Contexte.PlanningAnalyses.AsNoTracking().OrderBy(ligne => ligne.DateAnalyse).ToListAsync();
        Assert.Equal(2, lignes.Count);
        Assert.Equal(new DateOnly(2026, 9, 1), lignes[0].DateAnalyse);
        Assert.Equal(TypesAnalysePlanning.A, lignes[0].TypeAnalyse);
        Assert.Equal(TypesAnalysePlanning.D, lignes[1].TypeAnalyse);
        Assert.All(lignes, ligne =>
        {
            Assert.Equal(client.Id, ligne.IdClient);
            Assert.Equal(BaseDonneesTest.NomUtilisateurTest, ligne.CreePar);
            Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, ligne.DateCreation);
        });
    }

    [Fact]
    public async Task SauvegarderMoisAsync_CaseExistanteModifiee_MetAJourLeTypeSansDupliquer()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 1), TypesAnalysePlanning.A);
        _base.Horloge.Advance(TimeSpan.FromHours(1));

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, 1, TypesAnalysePlanning.C)));

        Assert.True(resultat.Reussi);
        var ligne = await _base.Contexte.PlanningAnalyses.AsNoTracking().SingleAsync();
        Assert.Equal(TypesAnalysePlanning.C, ligne.TypeAnalyse);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, ligne.DateCreation);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime.AddHours(1), ligne.DateModification);
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, ligne.ModifiePar);
    }

    [Fact]
    public async Task SauvegarderMoisAsync_CaseAbsenteDeLaSauvegarde_SupprimeLaLigne()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 1), TypesAnalysePlanning.A);
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 2), TypesAnalysePlanning.B);

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, 2, TypesAnalysePlanning.B)));

        Assert.True(resultat.Reussi);
        var ligne = await _base.Contexte.PlanningAnalyses.AsNoTracking().SingleAsync();
        Assert.Equal(new DateOnly(2026, 9, 2), ligne.DateAnalyse);
    }

    [Fact]
    public async Task SauvegarderMoisAsync_SauvegardeVide_SupprimeToutesLesCasesDuMois()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 9, 1), TypesAnalysePlanning.A);

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde());

        Assert.True(resultat.Reussi);
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Fact]
    public async Task SauvegarderMoisAsync_AutresMois_NeLesModifiePas()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 8, 31), TypesAnalysePlanning.A);
        await AjouterLigneAsync(client.Id, new DateOnly(2026, 10, 1), TypesAnalysePlanning.B);

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, 1, TypesAnalysePlanning.C)));

        Assert.True(resultat.Reussi);
        var dates = await _base.Contexte.PlanningAnalyses.AsNoTracking().OrderBy(ligne => ligne.DateAnalyse).Select(ligne => ligne.DateAnalyse).ToListAsync();
        Assert.Equal([new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)], dates);
    }

    [Fact]
    public async Task SauvegarderMoisAsync_PuisObtenirMois_RelitLesCasesSauvegardees()
    {
        var client = await AjouterClientAsync("Acier SA");
        var service = CreerService();
        await service.SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, 7, TypesAnalysePlanning.B)));

        var planning = await service.ObtenirMoisAsync(Annee, Mois);

        var caseplanning = Assert.Single(planning.Cases);
        Assert.Equal(new DateOnly(2026, 9, 7), caseplanning.Date);
        Assert.Equal(TypesAnalysePlanning.B, caseplanning.TypeAnalyse);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("AB")]
    public async Task SauvegarderMoisAsync_TypeAnalyseInvalide_RetourneEchecSansEnregistrer(string type)
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, 1, type)));

        Assert.False(resultat.Reussi);
        Assert.NotEmpty(resultat.Erreurs);
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public async Task SauvegarderMoisAsync_CaseUnWeekEnd_RetourneEchecSansEnregistrer(int jour)
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(client.Id, jour, TypesAnalysePlanning.A)));

        Assert.False(resultat.Reussi);
        Assert.Contains(resultat.Erreurs, erreur => erreur.Contains("week-end", StringComparison.Ordinal));
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Fact]
    public async Task SauvegarderMoisAsync_DateHorsDuMois_RetourneEchecSansEnregistrer()
    {
        var client = await AjouterClientAsync("Acier SA");
        var horsMois = new CasePlanningDto { IdClient = client.Id, Date = new DateOnly(2026, 10, 1), TypeAnalyse = TypesAnalysePlanning.A };

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(horsMois));

        Assert.False(resultat.Reussi);
        Assert.Contains("Toutes les dates doivent appartenir au mois planifié.", resultat.Erreurs);
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Fact]
    public async Task SauvegarderMoisAsync_DoublonClientEtJour_RetourneEchecSansEnregistrer()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(
            Case(client.Id, 1, TypesAnalysePlanning.A),
            Case(client.Id, 1, TypesAnalysePlanning.B)));

        Assert.False(resultat.Reussi);
        Assert.Contains("Un client ne peut avoir qu'un seul type d'analyse par jour.", resultat.Erreurs);
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Fact]
    public async Task SauvegarderMoisAsync_ClientInconnu_RetourneEchecSansEnregistrer()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(
            Case(client.Id, 1, TypesAnalysePlanning.A),
            Case(999, 1, TypesAnalysePlanning.A)));

        Assert.False(resultat.Reussi);
        Assert.Contains("Un des clients du planning n'existe plus.", resultat.Erreurs);
        Assert.False(await _base.Contexte.PlanningAnalyses.AnyAsync());
    }

    [Theory]
    [InlineData(1999, 9)]
    [InlineData(2101, 9)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task SauvegarderMoisAsync_PeriodeInvalide_RetourneEchec(int annee, int mois)
    {
        var resultat = await CreerService().SauvegarderMoisAsync(new SauvegardePlanningDto { Annee = annee, Mois = mois });

        Assert.False(resultat.Reussi);
    }

    [Fact]
    public async Task SauvegarderMoisAsync_ClientInvalide_RetourneEchec()
    {
        var resultat = await CreerService().SauvegarderMoisAsync(CreerSauvegarde(Case(0, 1, TypesAnalysePlanning.A)));

        Assert.False(resultat.Reussi);
    }

    private static CasePlanningDto Case(int idClient, int jour, string type)
        => new() { IdClient = idClient, Date = new DateOnly(Annee, Mois, jour), TypeAnalyse = type };

    private static SauvegardePlanningDto CreerSauvegarde(params CasePlanningDto[] cases)
        => new() { Annee = Annee, Mois = Mois, Cases = [.. cases] };

    private ServicePlanningAnalyses CreerService()
        => new(_base.Contexte, new ValidateurSauvegardePlanning(), _base.Horloge);

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return client;
    }

    private async Task AjouterLigneAsync(int idClient, DateOnly date, string type)
    {
        _base.Contexte.PlanningAnalyses.Add(new PlanningAnalyse { IdClient = idClient, DateAnalyse = date, TypeAnalyse = type });
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
    }
}
