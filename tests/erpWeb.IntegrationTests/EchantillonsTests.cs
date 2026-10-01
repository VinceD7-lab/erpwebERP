using System.Net;
using System.Text.Json;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Tournees;
using erpWeb.Infrastructure.Donnees;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.IntegrationTests;

/// <summary>Accès HTTP aux résultats d'échantillons et contraintes du schéma SQL Server réel.</summary>
public sealed class EchantillonsTests : IClassFixture<FabriqueApplication>
{
    private readonly FabriqueApplication _fabrique;

    public EchantillonsTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task Index_Administrateur_RetourneLaPage()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Echantillons");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_Administrateur_RetourneDuJsonPagine()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Echantillons/Resultats");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        var racine = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, racine.GetProperty("numeroPage").GetInt32());
        Assert.Equal(JsonValueKind.Array, racine.GetProperty("elements").ValueKind);
    }

    [Fact]
    public async Task Resultats_CompteAvecRoleUtilisateur_RetourneLaPage()
    {
        var client = CreerClient();
        var inscription = await client.EnvoyerFormulaireAsync("/Compte/Inscription", new Dictionary<string, string>
        {
            ["NomComplet"] = "Utilisateur Standard",
            ["Email"] = $"standard-{Guid.NewGuid():N}@erpweb.local",
            ["MotDePasse"] = "Standard#2026",
            ["ConfirmationMotDePasse"] = "Standard#2026",
        });
        Assert.Equal(HttpStatusCode.Redirect, inscription.StatusCode);

        var reponse = await client.GetAsync("/Echantillons/Resultats");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Theory]
    [InlineData("/Echantillons")]
    [InlineData("/Echantillons/Resultats")]
    public async Task Echantillons_CompteSansRole_RedirigeVersAccesRefuse(string url)
    {
        var client = await CreerClientSansPermissionAsync();

        var reponse = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/AccesRefuse", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Echantillons")]
    [InlineData("/Echantillons/Resultats")]
    public async Task Echantillons_Anonyme_RedirigeVersLaConnexion(string url)
    {
        var reponse = await CreerClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resultats_TriInconnu_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Echantillons/Resultats?tri=Nimportequoi");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_NumeroPageMaximal_RetourneUnePageVideSansErreurServeur()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync($"/Echantillons/Resultats?numeroPage={int.MaxValue}&taillePage=200");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_EchantillonEnBase_EstRestitueAvecSaDateDeTournee()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        await AjouterAsync(async contexte =>
        {
            var tournee = new TourneeRamassage { DateTournee = new DateOnly(2026, 3, 4), NomChauffeur = "Chauffeur Test" };
            contexte.Echantillons.Add(CreerEchantillon(code, tournee: tournee));
            await Task.CompletedTask;
        });
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetStringAsync("/Echantillons/Resultats?dateTourneeDebut=2026-03-04&dateTourneeFin=2026-03-04&taillePage=200");

        Assert.Contains(code, reponse, StringComparison.Ordinal);
        Assert.Contains("2026-03-04", reponse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enregistrement_CodeBarresDejaUtilise_EstRefuseParLIndexUnique()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        await AjouterAsync(contexte => contexte.Echantillons.Add(CreerEchantillon(code)));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AjouterAsync(contexte => contexte.Echantillons.Add(CreerEchantillon(code))));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("IX_Echantillons_CodeBarresAnonyme", erreurSql.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-10.01)]
    [InlineData(100.01)]
    [InlineData(150)]
    public async Task Enregistrement_TemperatureHorsBornes_EstRefuseParLaContrainteCheck(double temperature)
    {
        var echantillon = CreerEchantillon($"ECH-{Guid.NewGuid():N}", (decimal)temperature);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AjouterAsync(contexte => contexte.Echantillons.Add(echantillon)));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CK_Echantillons_TemperatureReception", erreurSql.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public async Task Enregistrement_TemperatureDansLesBornesInclusives_EstAcceptee(double temperature)
    {
        var code = $"ECH-{Guid.NewGuid():N}";

        await AjouterAsync(contexte => contexte.Echantillons.Add(CreerEchantillon(code, (decimal)temperature)));

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal((decimal)temperature, (await contexte.Echantillons.SingleAsync(echantillon => echantillon.CodeBarresAnonyme == code)).TemperatureReception);
    }

    [Fact]
    public async Task Enregistrement_TemperatureAbsente_EstAcceptee()
    {
        var code = $"ECH-{Guid.NewGuid():N}";

        await AjouterAsync(contexte => contexte.Echantillons.Add(CreerEchantillon(code)));

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Null((await contexte.Echantillons.SingleAsync(echantillon => echantillon.CodeBarresAnonyme == code)).TemperatureReception);
    }

    [Fact]
    public async Task Suppression_EchantillonAvecResultat_SupprimeLeResultatEnCascade()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        var echantillon = CreerEchantillon(code);
        echantillon.StatutAnalyse = StatutsAnalyse.Termine;
        echantillon.ResultatAgronomie = new ResultatAgronomie { TypeSupport = "Sol", PhSol = 6.5m };
        await AjouterAsync(contexte => contexte.Echantillons.Add(echantillon));
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiant = await contexte.Echantillons.Where(e => e.CodeBarresAnonyme == code).Select(e => e.Id).SingleAsync();
        Assert.Equal(1, await contexte.ResultatsAgronomie.CountAsync(resultat => resultat.IdEchantillon == identifiant));

        // Suppression directe en base : seule la cascade de la clé étrangère peut retirer le résultat.
        await contexte.Echantillons.Where(e => e.Id == identifiant).ExecuteDeleteAsync();

        Assert.Equal(0, await contexte.ResultatsAgronomie.CountAsync(resultat => resultat.IdEchantillon == identifiant));
    }

    [Fact]
    public async Task Enregistrement_DeuxResultatsPourUnEchantillon_EstRefuseParLIndexUnique()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        var echantillon = CreerEchantillon(code);
        echantillon.ResultatAgronomie = new ResultatAgronomie { TypeSupport = "Sol" };
        await AjouterAsync(contexte => contexte.Echantillons.Add(echantillon));
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiant = await contexte.Echantillons.Where(e => e.CodeBarresAnonyme == code).Select(e => e.Id).SingleAsync();
        contexte.ResultatsAgronomie.Add(new ResultatAgronomie { IdEchantillon = identifiant, TypeSupport = "Fourrage" });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());

        Assert.IsType<SqlException>(exception.InnerException);
    }

    [Fact]
    public async Task Suppression_TourneeAvecEchantillons_EstRefuseParLaCleEtrangere()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        var tournee = new TourneeRamassage { DateTournee = new DateOnly(2026, 4, 1), NomChauffeur = "Chauffeur Test" };
        await AjouterAsync(contexte => contexte.Echantillons.Add(CreerEchantillon(code, tournee: tournee)));
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiantTournee = await contexte.Echantillons.Where(e => e.CodeBarresAnonyme == code).Select(e => e.IdTournee!.Value).SingleAsync();

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => contexte.TourneesRamassage.Where(t => t.Id == identifiantTournee).ExecuteDeleteAsync());

        Assert.Contains("FK_Echantillons_TourneesRamassage_IdTournee", exception.Message, StringComparison.Ordinal);
        Assert.True(await contexte.Echantillons.AnyAsync(e => e.CodeBarresAnonyme == code && e.IdTournee == identifiantTournee));
        Assert.True(await contexte.TourneesRamassage.AnyAsync(t => t.Id == identifiantTournee));
    }

    [Fact]
    public async Task Suppression_TourneeSansEchantillon_Reussit()
    {
        var tournee = new TourneeRamassage { DateTournee = new DateOnly(2026, 4, 2), NomChauffeur = "Chauffeur Test" };
        await AjouterAsync(contexte => contexte.TourneesRamassage.Add(tournee));
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;

        var supprimees = await contexte.TourneesRamassage.Where(t => t.Id == tournee.Id).ExecuteDeleteAsync();

        Assert.Equal(1, supprimees);
    }

    private static Echantillon CreerEchantillon(string code, decimal? temperature = null, TourneeRamassage? tournee = null)
        => new()
        {
            CodeBarresAnonyme = code,
            DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
            Filiere = "Grandes cultures",
            StatutAnalyse = StatutsAnalyse.Recu,
            TemperatureReception = temperature,
            Tournee = tournee,
        };

    private async Task AjouterAsync(Action<AppDbContext> ajout)
        => await AjouterAsync(contexte =>
        {
            ajout(contexte);
            return Task.CompletedTask;
        });

    private async Task AjouterAsync(Func<AppDbContext, Task> ajout)
    {
        using var portee = _fabrique.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        await ajout(contexte);
        await contexte.SaveChangesAsync();
    }

    private AppDbContext CreerContexte(out IServiceScope portee)
    {
        portee = _fabrique.Services.CreateScope();
        return portee.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private HttpClient CreerClient()
        => _fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> CreerClientConnecteAsync()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);
        return client;
    }

    /// <summary>Compte créé par l'administrateur sans aucun rôle, donc sans permission.</summary>
    private async Task<HttpClient> CreerClientSansPermissionAsync()
    {
        var email = $"sans-role-{Guid.NewGuid():N}@erpweb.local";
        const string motDePasse = "SansRole#2026";

        var administrateur = await CreerClientConnecteAsync();
        var creation = await administrateur.EnvoyerFormulaireAsync("/Utilisateurs/Creer", new Dictionary<string, string>
        {
            ["NomComplet"] = "Compte Sans Role",
            ["Email"] = email,
            ["MotDePasse"] = motDePasse,
        });
        Assert.Equal(HttpStatusCode.Redirect, creation.StatusCode);

        var client = CreerClient();
        await client.ConnecterAsync(email, motDePasse);
        return client;
    }
}
