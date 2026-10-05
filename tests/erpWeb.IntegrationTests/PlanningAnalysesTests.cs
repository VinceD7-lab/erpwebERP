using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace erpWeb.IntegrationTests;

public sealed class PlanningAnalysesTests : IClassFixture<FabriqueApplication>
{
    private const string EnTeteJeton = "RequestVerificationToken";

    private readonly FabriqueApplication _fabrique;

    public PlanningAnalysesTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Theory]
    [InlineData("/PlanningAnalyses")]
    [InlineData("/PlanningAnalyses/Mois?annee=2031&mois=3")]
    public async Task Planning_UtilisateurAnonyme_RedirigeVersLaConnexion(string url)
    {
        var reponse = await CreerClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_Administrateur_AfficheLaPageAvecLeJetonEtLeComposant()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/PlanningAnalyses?annee=2031&mois=3");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var html = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("data-composant-vue=\"planning-analyses\"", html, StringComparison.Ordinal);
        Assert.Contains("data-annee=\"2031\"", html, StringComparison.Ordinal);
        Assert.Contains("data-mois=\"3\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"__RequestVerificationToken\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mois_Administrateur_RetourneLesJoursEnJson()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/PlanningAnalyses/Mois?annee=2031&mois=3");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync());
        Assert.Equal(2031, document.RootElement.GetProperty("annee").GetInt32());
        Assert.Equal(3, document.RootElement.GetProperty("mois").GetInt32());
        Assert.Equal(31, document.RootElement.GetProperty("jours").GetArrayLength());
        Assert.Equal(0, document.RootElement.GetProperty("cases").GetArrayLength());
    }

    [Fact]
    public async Task Sauvegarder_JsonAvecJetonEnEnTete_EnregistrePuisLeMoisRelitLaCase()
    {
        var client = await CreerClientConnecteAsync();
        var idClient = await CreerClientMetierAsync(client);

        var reponse = await EnvoyerSauvegardeAsync(client, new
        {
            annee = 2031,
            mois = 4,
            cases = new[] { new { idClient, date = "2031-04-01", typeAnalyse = "B" } },
        });

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/PlanningAnalyses/Mois?annee=2031&mois=4"));
        var cases = document.RootElement.GetProperty("cases");
        Assert.Equal(1, cases.GetArrayLength());
        Assert.Equal(idClient, cases[0].GetProperty("idClient").GetInt32());
        Assert.Equal("2031-04-01", cases[0].GetProperty("date").GetString());
        Assert.Equal("B", cases[0].GetProperty("typeAnalyse").GetString());
    }

    [Fact]
    public async Task Sauvegarder_WeekEnd_Retourne400AvecLesErreurs()
    {
        var client = await CreerClientConnecteAsync();
        var idClient = await CreerClientMetierAsync(client);

        // Le 5 avril 2031 est un samedi.
        var reponse = await EnvoyerSauvegardeAsync(client, new
        {
            annee = 2031,
            mois = 4,
            cases = new[] { new { idClient, date = "2031-04-05", typeAnalyse = "A" } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("week-end", await reponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sauvegarder_SansJetonAntiforgery_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.PostAsJsonAsync("/PlanningAnalyses/Sauvegarder", new { annee = 2031, mois = 5, cases = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    private HttpClient CreerClient()
        => _fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> CreerClientConnecteAsync()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);
        return client;
    }

    /// <summary>Crée un client et retourne son identifiant, lu dans le JSON du planning.</summary>
    private static async Task<int> CreerClientMetierAsync(HttpClient client)
    {
        var raisonSociale = $"Client {Guid.NewGuid():N}";
        var creation = await client.EnvoyerFormulaireAsync("/Clients/Creer", new Dictionary<string, string> { ["RaisonSociale"] = raisonSociale });
        Assert.Equal(HttpStatusCode.Redirect, creation.StatusCode);

        using var document = JsonDocument.Parse(await client.GetStringAsync("/PlanningAnalyses/Mois?annee=2031&mois=3"));
        var correspondance = document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(element => element.GetProperty("raisonSociale").GetString() == raisonSociale);
        return correspondance.GetProperty("id").GetInt32();
    }

    /// <summary>Reproduit l'appel de planning-analyses.js : corps JSON, jeton antiforgery dans l'en-tête.</summary>
    private static async Task<HttpResponseMessage> EnvoyerSauvegardeAsync(HttpClient client, object corps)
    {
        var jeton = await client.ObtenirJetonAntiforgeryAsync("/PlanningAnalyses");
        using var requete = new HttpRequestMessage(HttpMethod.Post, "/PlanningAnalyses/Sauvegarder")
        {
            Content = JsonContent.Create(corps),
        };
        requete.Headers.Add(EnTeteJeton, jeton);
        return await client.SendAsync(requete);
    }
}
