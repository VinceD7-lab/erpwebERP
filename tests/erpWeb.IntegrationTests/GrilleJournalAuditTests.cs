using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace erpWeb.IntegrationTests;

/// <summary>Contrat de l'endpoint JSON consommé par la grille du journal d'audit.</summary>
public sealed class GrilleJournalAuditTests : IClassFixture<FabriqueApplication>
{
    private readonly FabriqueApplication _fabrique;

    public GrilleJournalAuditTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task Entrees_Administrateur_RetourneDuJsonPagine()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/JournalAudit/Entrees");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);

        var racine = await LireRacineAsync(reponse);
        Assert.Equal(1, racine.GetProperty("numeroPage").GetInt32());
        Assert.True(racine.GetProperty("nombrePages").GetInt32() >= 1);
        Assert.True(racine.GetProperty("nombreTotal").GetInt32() >= 0);
        Assert.Equal(JsonValueKind.Array, racine.GetProperty("elements").ValueKind);
    }

    [Fact]
    public async Task Entrees_Administrateur_SerialiseLActionEnTexte()
    {
        var client = await CreerClientConnecteAsync();

        var racine = await LireRacineAsync(await client.GetAsync("/JournalAudit/Entrees"));

        var elements = racine.GetProperty("elements").EnumerateArray().ToList();
        Assert.NotEmpty(elements);
        Assert.All(elements, element => Assert.Equal(JsonValueKind.String, element.GetProperty("action").ValueKind));
    }

    [Fact]
    public async Task Entrees_TaillePageExcessive_EstRamenee()
    {
        var client = await CreerClientConnecteAsync();

        var racine = await LireRacineAsync(await client.GetAsync("/JournalAudit/Entrees?taillePage=5000"));

        Assert.Equal(200, racine.GetProperty("taillePage").GetInt32());
    }

    [Fact]
    public async Task Entrees_TriInvalide_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/JournalAudit/Entrees?tri=Nimportequoi");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Entrees_Anonyme_AvecAcceptJson_Retourne401()
    {
        var client = CreerClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reponse = await client.GetAsync("/JournalAudit/Entrees");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Entrees_InscritSansPermission_AvecAcceptJson_Retourne403()
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
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reponse = await client.GetAsync("/JournalAudit/Entrees");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Entrees_Anonyme_SansAcceptJson_RedirigeVersLaConnexion()
    {
        var reponse = await CreerClient().GetAsync("/JournalAudit/Entrees");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    private static async Task<JsonElement> LireRacineAsync(HttpResponseMessage reponse)
        => JsonDocument.Parse(await reponse.Content.ReadAsStringAsync()).RootElement;

    private HttpClient CreerClient()
        => _fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> CreerClientConnecteAsync()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);
        return client;
    }
}
