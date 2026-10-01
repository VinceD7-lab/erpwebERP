using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Tournees;
using erpWeb.Infrastructure.Donnees;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class GenerateurEchantillonsDemonstrationTests : IDisposable
{
    private const int NombreTournees = 12;

    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task GenererAsync_AvecClients_GenereLeNombreDeTourneesDemande()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        Assert.Equal(NombreTournees, await _base.Contexte.TourneesRamassage.CountAsync());
        Assert.True(await _base.Contexte.Echantillons.CountAsync(echantillon => echantillon.IdTournee != null) >= NombreTournees * 2);
    }

    [Fact]
    public async Task GenererAsync_AvecClients_GenereTroisEchantillonsSansTourneeNiClient()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var sansRattachement = await _base.Contexte.Echantillons
            .Where(echantillon => echantillon.IdTournee == null || echantillon.IdClient == null)
            .ToListAsync();
        Assert.Equal(3, sansRattachement.Count);
        Assert.All(sansRattachement, echantillon =>
        {
            Assert.Null(echantillon.IdTournee);
            Assert.Null(echantillon.IdClient);
            Assert.Equal(StatutsAnalyse.Recu, echantillon.StatutAnalyse);
            Assert.Null(echantillon.ResultatAgronomie);
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesTourneesSontSurDesDatesDistinctesDuPasseEnSemaine()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var dates = await _base.Contexte.TourneesRamassage.Select(tournee => tournee.DateTournee).ToListAsync();
        var aujourdhui = DateOnly.FromDateTime(_base.Horloge.GetUtcNow().UtcDateTime);
        Assert.Equal(dates.Count, dates.Distinct().Count());
        Assert.All(dates, date =>
        {
            Assert.True(date <= aujourdhui);
            Assert.NotEqual(DayOfWeek.Saturday, date.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Sunday, date.DayOfWeek);
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LePrelevementPrecedeOuCoincideAvecLaTournee()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var echantillons = await _base.Contexte.Echantillons.Include(echantillon => echantillon.Tournee)
            .Where(echantillon => echantillon.Tournee != null)
            .ToListAsync();
        Assert.NotEmpty(echantillons);
        Assert.All(echantillons, echantillon =>
            Assert.True(
                DateOnly.FromDateTime(echantillon.DatePrelevement) <= echantillon.Tournee!.DateTournee,
                $"Prélèvement {echantillon.DatePrelevement:O} postérieur à la tournée {echantillon.Tournee.DateTournee}."));
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LaTemperatureRespecteLesBornesEtLeStatutDeTournee()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var echantillons = await _base.Contexte.Echantillons.Include(echantillon => echantillon.Tournee).ToListAsync();
        Assert.All(echantillons, echantillon =>
        {
            Assert.NotNull(echantillon.TemperatureReception);
            Assert.InRange(echantillon.TemperatureReception!.Value, LongueursEchantillon.TemperatureMinimale, LongueursEchantillon.TemperatureMaximale);

            if (echantillon.Tournee?.StatutTemperature == StatutsTemperature.Alerte)
            {
                Assert.InRange(echantillon.TemperatureReception.Value, 9m, 15m);
            }
            else
            {
                Assert.InRange(echantillon.TemperatureReception.Value, 2m, 8m);
            }
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesStatutsDeTourneeSontConnus()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var statuts = await _base.Contexte.TourneesRamassage.Select(tournee => tournee.StatutTemperature).ToListAsync();
        Assert.All(statuts, statut => Assert.Contains(statut, StatutsTemperature.Tous));
    }

    [Fact]
    public async Task GenererAsync_AvecClients_SeulsLesEchantillonsTerminesOntUnResultat()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var echantillons = await _base.Contexte.Echantillons.Include(echantillon => echantillon.ResultatAgronomie).ToListAsync();
        Assert.Contains(echantillons, echantillon => echantillon.StatutAnalyse == StatutsAnalyse.Termine);
        Assert.All(echantillons, echantillon =>
        {
            Assert.Contains(echantillon.StatutAnalyse, StatutsAnalyse.Tous);
            Assert.Equal(echantillon.StatutAnalyse == StatutsAnalyse.Termine, echantillon.ResultatAgronomie is not null);
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LaValidationSuitLaTourneeSansEtreDansLeFutur()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var echantillons = await _base.Contexte.Echantillons
            .Include(echantillon => echantillon.Tournee)
            .Include(echantillon => echantillon.ResultatAgronomie)
            .Where(echantillon => echantillon.ResultatAgronomie != null)
            .ToListAsync();
        var maintenant = _base.Horloge.GetUtcNow().UtcDateTime;
        Assert.NotEmpty(echantillons);
        Assert.All(echantillons, echantillon =>
        {
            var validation = echantillon.ResultatAgronomie!.DateValidation;
            Assert.NotNull(validation);
            Assert.True(DateOnly.FromDateTime(validation.Value) >= echantillon.Tournee!.DateTournee);
            Assert.True(validation.Value <= maintenant);
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesResultatsSontCoherentsAvecLeTypeDeSupport()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var resultats = await _base.Contexte.ResultatsAgronomie.Include(resultat => resultat.Echantillon).ToListAsync();
        Assert.NotEmpty(resultats);
        Assert.All(resultats, resultat =>
        {
            if (resultat.Echantillon!.Filiere == "Elevage")
            {
                Assert.Equal("Fourrage", resultat.TypeSupport);
                Assert.NotNull(resultat.ValeurUclFourrage);
                Assert.Null(resultat.PhSol);
            }
            else
            {
                Assert.Equal("Sol", resultat.TypeSupport);
                Assert.NotNull(resultat.PhSol);
                Assert.Null(resultat.ValeurUclFourrage);
            }
        });
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesCodesBarresSontUniquesEtDansLaLimiteDeLongueur()
    {
        await AjouterClientsAsync(6);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var codes = await _base.Contexte.Echantillons.Select(echantillon => echantillon.CodeBarresAnonyme).ToListAsync();
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.All(codes, code => Assert.InRange(code.Length, 1, LongueursEchantillon.CodeBarresAnonyme));
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesEchantillonsRattachesUtilisentDesClientsExistants()
    {
        await AjouterClientsAsync(2);

        await CreerGenerateur().GenererAsync(NombreTournees);

        var identifiantsClients = await _base.Contexte.Clients.Select(client => client.Id).ToListAsync();
        var identifiantsUtilises = await _base.Contexte.Echantillons
            .Where(echantillon => echantillon.IdClient != null)
            .Select(echantillon => echantillon.IdClient!.Value)
            .Distinct()
            .ToListAsync();
        Assert.NotEmpty(identifiantsUtilises);
        Assert.All(identifiantsUtilises, identifiant => Assert.Contains(identifiant, identifiantsClients));
    }

    [Fact]
    public async Task GenererAsync_AppelsSuccessifs_LeSecondAppelNAjouteRien()
    {
        await AjouterClientsAsync(6);
        var generateur = CreerGenerateur();
        await generateur.GenererAsync(NombreTournees);
        var tourneesAvant = await _base.Contexte.TourneesRamassage.CountAsync();
        var echantillonsAvant = await _base.Contexte.Echantillons.CountAsync();
        var resultatsAvant = await _base.Contexte.ResultatsAgronomie.CountAsync();

        await generateur.GenererAsync(NombreTournees);

        Assert.Equal(tourneesAvant, await _base.Contexte.TourneesRamassage.CountAsync());
        Assert.Equal(echantillonsAvant, await _base.Contexte.Echantillons.CountAsync());
        Assert.Equal(resultatsAvant, await _base.Contexte.ResultatsAgronomie.CountAsync());
    }

    [Fact]
    public async Task GenererAsync_TourneeDejaPresente_NeGenereRien()
    {
        await AjouterClientsAsync(3);
        _base.Contexte.TourneesRamassage.Add(new TourneeRamassage { DateTournee = new DateOnly(2026, 9, 1), NomChauffeur = "Chauffeur Test" });
        await _base.Contexte.SaveChangesAsync();

        await CreerGenerateur().GenererAsync(NombreTournees);

        Assert.Equal(1, await _base.Contexte.TourneesRamassage.CountAsync());
        Assert.Equal(0, await _base.Contexte.Echantillons.CountAsync());
    }

    [Fact]
    public async Task GenererAsync_SansClient_NeGenereRien()
    {
        await CreerGenerateur().GenererAsync(NombreTournees);

        Assert.Equal(0, await _base.Contexte.TourneesRamassage.CountAsync());
        Assert.Equal(0, await _base.Contexte.Echantillons.CountAsync());
        Assert.Equal(0, await _base.Contexte.ResultatsAgronomie.CountAsync());
    }

    [Fact]
    public async Task GenererAsync_MemeHorlogeSurDeuxBases_ProduitLesMemesDonnees()
    {
        await AjouterClientsAsync(6);
        await CreerGenerateur().GenererAsync(NombreTournees);
        var premiere = await _base.Contexte.Echantillons.OrderBy(echantillon => echantillon.CodeBarresAnonyme)
            .Select(echantillon => echantillon.CodeBarresAnonyme + "|" + echantillon.Filiere + "|" + echantillon.StatutAnalyse)
            .ToListAsync();

        using var autreBase = new BaseDonneesTest();
        for (var index = 0; index < 6; index++)
        {
            autreBase.Contexte.Clients.Add(new Client { RaisonSociale = $"Client {index}" });
        }

        await autreBase.Contexte.SaveChangesAsync();
        await new GenerateurEchantillonsDemonstration(autreBase.Contexte, autreBase.Horloge, NullLogger<GenerateurEchantillonsDemonstration>.Instance)
            .GenererAsync(NombreTournees);
        var seconde = await autreBase.Contexte.Echantillons.OrderBy(echantillon => echantillon.CodeBarresAnonyme)
            .Select(echantillon => echantillon.CodeBarresAnonyme + "|" + echantillon.Filiere + "|" + echantillon.StatutAnalyse)
            .ToListAsync();

        Assert.Equal(premiere, seconde);
    }

    [Fact]
    public async Task GenererAsync_PeuDeJoursDisponibles_NeDepassePasLeNombreDeDatesPossibles()
    {
        await AjouterClientsAsync(3);

        await CreerGenerateur().GenererAsync(nombreTournees: 500);

        var nombre = await _base.Contexte.TourneesRamassage.CountAsync();
        Assert.InRange(nombre, 1, 60);
    }

    private GenerateurEchantillonsDemonstration CreerGenerateur()
        => new(_base.Contexte, _base.Horloge, NullLogger<GenerateurEchantillonsDemonstration>.Instance);

    private async Task AjouterClientsAsync(int quantite)
    {
        for (var index = 0; index < quantite; index++)
        {
            _base.Contexte.Clients.Add(new Client { RaisonSociale = $"Client {index}" });
        }

        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
    }
}
