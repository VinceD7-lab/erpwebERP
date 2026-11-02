using System.Net;
using System.Text.Json;
using erpWeb.Core.Clients;
using erpWeb.Core.DevisClients;
using erpWeb.Infrastructure.Donnees;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.IntegrationTests;

/// <summary>Accès HTTP aux devis et contraintes du schéma SQL Server réel.</summary>
public sealed class DevisTests : IClassFixture<FabriqueApplication>
{
    private readonly FabriqueApplication _fabrique;

    public DevisTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task Index_Administrateur_RetourneLaPage()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Devis");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains("data-composant-vue=\"tableau-devis\"", await reponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_Anonyme_RedirigeVersLaConnexion()
    {
        var reponse = await CreerClient().GetAsync("/Devis");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_CompteSansPermission_RedirigeVersAccesRefuse()
    {
        var client = await _fabrique.CreerClientSansPermissionAsync();

        var reponse = await client.GetAsync("/Devis");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/AccesRefuse", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resultats_Administrateur_RetourneDuJsonPagine()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Devis/Resultats");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        var racine = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, racine.GetProperty("numeroPage").GetInt32());
        Assert.Equal(JsonValueKind.Array, racine.GetProperty("elements").ValueKind);

        // Clés lues par ajaxResponse dans tableau-devis.js.
        Assert.True(racine.GetProperty("nombrePages").GetInt32() >= 1);
        Assert.True(racine.GetProperty("nombreTotal").GetInt32() >= 0);
    }

    [Fact]
    public async Task Resultats_DevisEnBase_EstRestitueAvecSonClientEtSonStatutEnTexte()
    {
        var raisonSociale = $"Client {Guid.NewGuid():N}";
        var numero = await AjouterDevisAsync(raisonSociale);
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetStringAsync($"/Devis/Resultats?recherche={numero}");

        var element = JsonDocument.Parse(reponse).RootElement.GetProperty("elements").EnumerateArray().Single();
        Assert.Equal(numero, element.GetProperty("numero").GetString());
        Assert.Equal(raisonSociale, element.GetProperty("raisonSocialeClient").GetString());
        Assert.Equal("Envoye", element.GetProperty("statut").GetString());
    }

    [Fact]
    public async Task Resultats_RechercheSurRaisonSociale_FiltreLesDevis()
    {
        var raisonSociale = $"Client {Guid.NewGuid():N}";
        await AjouterDevisAsync(raisonSociale);
        await AjouterDevisAsync($"Autre {Guid.NewGuid():N}");
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetStringAsync($"/Devis/Resultats?recherche={raisonSociale}");

        var racine = JsonDocument.Parse(reponse).RootElement;
        Assert.Equal(1, racine.GetProperty("nombreTotal").GetInt32());
    }

    [Fact]
    public async Task Resultats_RechercheEnMinusculesEtAvecEspaces_TrouveLeClient()
    {
        var raisonSociale = $"Client {Guid.NewGuid():N}";
        await AjouterDevisAsync(raisonSociale);
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetStringAsync($"/Devis/Resultats?recherche=%20{raisonSociale.ToLowerInvariant()}%20");

        Assert.Equal(1, JsonDocument.Parse(reponse).RootElement.GetProperty("nombreTotal").GetInt32());
    }

    [Fact]
    public async Task Resultats_TriParMontantDescendant_RetourneLaPage()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Devis/Resultats?tri=MontantTotalToutesTaxesComprises&triDescendant=true");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_NumeroPageMaximal_RetourneUnePageVideSansErreurServeur()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync($"/Devis/Resultats?numeroPage={int.MaxValue}&taillePage=200");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_TriInconnu_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Devis/Resultats?tri=Nimportequoi");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_Anonyme_RedirigeVersLaConnexion()
    {
        var reponse = await CreerClient().GetAsync("/Devis/Resultats");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
    }

    [Fact]
    public async Task Enregistrement_NumeroDejaUtilise_EstRefuseParLaBase()
    {
        var numero = $"DEV-{Guid.NewGuid():N}"[..LongueursDevis.Numero];
        await AjouterDevisAsync($"Client {Guid.NewGuid():N}", numero);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AjouterDevisAsync($"Client {Guid.NewGuid():N}", numero));

        Assert.IsType<SqlException>(exception.InnerException);
    }

    [Fact]
    public async Task Suppression_ClientAvecDevis_EstRefuseeParLaBase()
    {
        var numero = $"DEV-{Guid.NewGuid():N}"[..LongueursDevis.Numero];
        await AjouterDevisAsync($"Client {Guid.NewGuid():N}", numero);
        using var portee = _fabrique.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var proprietaire = await contexte.Clients.SingleAsync(client => contexte.Devis.Any(devis => devis.Numero == numero && devis.ClientId == client.Id));
        contexte.Clients.Remove(proprietaire);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());

        Assert.IsType<SqlException>(exception.InnerException);
    }

    /// <summary>Crée un client et un devis envoyé d'une ligne ; retourne le numéro du devis.</summary>
    private async Task<string> AjouterDevisAsync(string raisonSociale, string? numero = null)
    {
        numero ??= $"DEV-{Guid.NewGuid():N}"[..LongueursDevis.Numero];
        using var portee = _fabrique.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var devis = new Devis
        {
            Numero = numero,
            Client = new Client { RaisonSociale = raisonSociale },
            DateDevis = new DateTime(2026, 3, 4),
            DateValidite = new DateTime(2026, 4, 3),
            Statut = StatutDevis.Envoye,
            Objet = "Analyse de sol",
            Lignes = [new LigneDevis { NumeroLigne = 1, Designation = "Prestation", Quantite = 2, PrixUnitaireHorsTaxe = 50m, TauxTaxe = 20m }],
        };
        portee.ServiceProvider.GetRequiredService<ICalculateurDevis>().Calculer(devis);

        contexte.Devis.Add(devis);
        await contexte.SaveChangesAsync();
        return numero;
    }

    private HttpClient CreerClient()
        => _fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<HttpClient> CreerClientConnecteAsync()
    {
        var client = CreerClient();
        await client.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);
        return client;
    }
}
