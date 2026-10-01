using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace erpWeb.IntegrationTests;

public sealed class EditionLigneClientsTests : IClassFixture<FabriqueApplication>
{
    private const string EnTeteJeton = "RequestVerificationToken";

    private readonly FabriqueApplication _fabrique;

    public EditionLigneClientsTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task Liste_ClientExistant_AfficheLaRaisonSocialeCommeLienVersLEcranDeModification()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, _) = await CreerClientMetierAsync(client);

        var liste = await client.GetStringAsync("/Clients");

        Assert.Matches($"<a href=\"/Clients/Modifier/{identifiant}\">Client ", liste);
        Assert.Contains("data-editer-ligne", liste, StringComparison.Ordinal);
        Assert.Contains("name=\"__RequestVerificationToken\"", liste, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LigneEdition_ClientExistant_RetourneLesChampsAffichesEtLesChampsCaches()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, _) = await CreerClientMetierAsync(client);

        var reponse = await client.GetAsync($"/Clients/LigneEdition/{identifiant}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var html = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("name=\"RaisonSociale\"", html, StringComparison.Ordinal);
        Assert.Contains("data-enregistrer-ligne", html, StringComparison.Ordinal);
        Assert.Contains("data-annuler-ligne", html, StringComparison.Ordinal);
        Assert.Contains("value=\"12 rue des Forges\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"0607080910\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"0102030405\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LigneEdition_ClientInexistant_Retourne404()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Clients/LigneEdition/999999");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task LigneEdition_UtilisateurAnonyme_RedirigeVersLaConnexion()
    {
        var reponse = await CreerClient().GetAsync("/Clients/LigneEdition/1");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LigneLecture_ClientExistant_RetourneLaLigneEnLecture()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, raisonSociale) = await CreerClientMetierAsync(client);

        var reponse = await client.GetAsync($"/Clients/LigneLecture/{identifiant}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var html = await reponse.Content.ReadAsStringAsync();
        Assert.Contains(raisonSociale, html, StringComparison.Ordinal);
        Assert.Contains("data-editer-ligne", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-enregistrer-ligne", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModifierLigne_SaisieValide_RetourneLaLigneMiseAJourEtConserveLesChampsNonAffiches()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, raisonSociale) = await CreerClientMetierAsync(client);

        var reponse = await EnvoyerLigneAsync(client, new Dictionary<string, string>
        {
            ["Id"] = identifiant,
            ["RaisonSociale"] = raisonSociale,
            ["Ville"] = "Lyon",
            ["CodePostal"] = "69001",
            ["Pays"] = "France",
            ["Telephone1"] = "04.72.00.00.00",
            ["Adresse1"] = "12 rue des Forges",
            ["Telephone2"] = "0607080910",
        });

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var ligne = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("Lyon", ligne, StringComparison.Ordinal);
        Assert.Contains("0472000000", ligne, StringComparison.Ordinal);
        Assert.Contains("data-editer-ligne", ligne, StringComparison.Ordinal);

        var edition = await client.GetStringAsync($"/Clients/LigneEdition/{identifiant}");
        Assert.Contains("value=\"12 rue des Forges\"", edition, StringComparison.Ordinal);
        Assert.Contains("value=\"0607080910\"", edition, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModifierLigne_SaisieInvalide_Retourne422AvecLaLigneEnEdition()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, _) = await CreerClientMetierAsync(client);

        var reponse = await EnvoyerLigneAsync(client, new Dictionary<string, string>
        {
            ["Id"] = identifiant,
            ["RaisonSociale"] = string.Empty,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        var html = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("data-enregistrer-ligne", html, StringComparison.Ordinal);
        Assert.Contains("text-danger", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModifierLigne_SansJetonAntiforgery_Retourne400()
    {
        var client = await CreerClientConnecteAsync();
        var (identifiant, raisonSociale) = await CreerClientMetierAsync(client);

        var reponse = await client.PostAsync("/Clients/ModifierLigne", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = identifiant,
            ["RaisonSociale"] = raisonSociale,
        }));

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

    /// <summary>Crée un client avec des champs absents de la liste (adresse, téléphone secondaire) et retourne son identifiant.</summary>
    private static async Task<(string Identifiant, string RaisonSociale)> CreerClientMetierAsync(HttpClient client)
    {
        var raisonSociale = $"Client {Guid.NewGuid():N}";
        var creation = await client.EnvoyerFormulaireAsync("/Clients/Creer", new Dictionary<string, string>
        {
            ["RaisonSociale"] = raisonSociale,
            ["Adresse1"] = "12 rue des Forges",
            ["CodePostal"] = "75001",
            ["Ville"] = "Paris",
            ["Pays"] = "France",
            ["Telephone1"] = "0102030405",
            ["Telephone2"] = "0607080910",
        });
        Assert.Equal(HttpStatusCode.Redirect, creation.StatusCode);

        var liste = await client.GetStringAsync("/Clients");
        var correspondance = Regex.Match(liste, $"data-id=\"(\\d+)\"[^>]*>\\s*<td>\\s*<a href=\"[^\"]*\">{Regex.Escape(raisonSociale)}</a>");
        Assert.True(correspondance.Success, $"Ligne du client {raisonSociale} introuvable dans la liste.");
        return (correspondance.Groups[1].Value, raisonSociale);
    }

    /// <summary>Reproduit l'appel de site.js : POST multipart-like, jeton antiforgery dans l'en-tête.</summary>
    private static async Task<HttpResponseMessage> EnvoyerLigneAsync(HttpClient client, Dictionary<string, string> champs)
    {
        var jeton = await client.ObtenirJetonAntiforgeryAsync("/Clients");
        using var requete = new HttpRequestMessage(HttpMethod.Post, "/Clients/ModifierLigne")
        {
            Content = new FormUrlEncodedContent(champs),
        };
        requete.Headers.Add(EnTeteJeton, jeton);
        requete.Headers.Add("X-Requested-With", "fetch");
        return await client.SendAsync(requete);
    }
}
