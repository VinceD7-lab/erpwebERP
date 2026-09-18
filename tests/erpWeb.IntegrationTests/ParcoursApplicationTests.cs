using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace erpWeb.IntegrationTests;

public sealed class ParcoursApplicationTests : IClassFixture<FabriqueApplication>
{
    private readonly FabriqueApplication _fabrique;

    public ParcoursApplicationTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task TableauDeBord_UtilisateurAnonyme_RedirigeVersLaConnexion()
    {
        var reponse = await CreerClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Compte/Connexion")]
    [InlineData("/Compte/Inscription")]
    [InlineData("/css/site.css")]
    public async Task PagesPubliques_UtilisateurAnonyme_SontAccessibles(string url)
    {
        var reponse = await CreerClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task TableauDeBord_Administrateur_AfficheLesQuatreWidgets()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);

        var html = await client.GetStringAsync("/");

        foreach (var widget in new[] { "utilisateurs-actifs", "activite-audit", "derniers-documents", "raccourcis" })
        {
            Assert.Contains($"data-widget=\"{widget}\"", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RafraichissementWidget_Administrateur_RetourneLeGraphiqueDActivite()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);

        var reponse = await client.GetAsync("/TableauDeBord/Widget/activite-audit");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("data-graphique", await reponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportJournalAudit_Administrateur_RetourneUnFichierExcel()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);

        var reponse = await client.GetAsync("/JournalAudit/Exporter");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", reponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Utilisateurs_CompteInscritSansPermission_RedirigeVersAccesRefuse()
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

        var reponse = await client.GetAsync("/Utilisateurs");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/AccesRefuse", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    private HttpClient CreerClient()
        => _fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}
