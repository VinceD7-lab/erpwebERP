using System.Net;
using System.Text.Json;
using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.Infrastructure.Donnees;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.IntegrationTests;

/// <summary>Accès HTTP aux factures et contraintes du schéma SQL Server réel.</summary>
public sealed class FacturesTests : IClassFixture<FabriqueApplication>
{
    private readonly FabriqueApplication _fabrique;

    public FacturesTests(FabriqueApplication fabrique)
    {
        _fabrique = fabrique;
    }

    [Fact]
    public async Task Index_Administrateur_RetourneLaPage()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Index_EchantillonAvecClientSansFacture_NeCreeAucuneFacture()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal(0, await contexte.Factures.CountAsync(facture => facture.IdEchantillon == idEchantillon));
    }

    [Fact]
    public async Task Generer_EchantillonAvecClientSansFacture_GenereLaFactureEtRedirigeVersIndex()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var client = await CreerClientConnecteAsync();

        var reponse = await PosterGenererAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Equal("/Factures", reponse.Headers.Location!.ToString().TrimEnd('/'), ignoreCase: true);
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal(1, await contexte.Factures.CountAsync(facture => facture.IdEchantillon == idEchantillon));
    }

    [Fact]
    public async Task Generer_AppelsRepetes_NeDupliquentPasLesFactures()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var client = await CreerClientConnecteAsync();

        await PosterGenererAsync(client);
        await PosterGenererAsync(client);

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal(1, await contexte.Factures.CountAsync(facture => facture.IdEchantillon == idEchantillon));
    }

    [Fact]
    public async Task Generer_Anonyme_RedirigeVersLaConnexionSansCreerDeFacture()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var anonyme = CreerClient();
        var jeton = await anonyme.ObtenirJetonAntiforgeryAsync("/Compte/Connexion");

        var reponse = await anonyme.PostAsync("/Factures/Generer", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = jeton }));

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
        Assert.False(await ExisteFactureAsync(idEchantillon));
    }

    [Fact]
    public async Task Generer_CompteSansPermission_RedirigeVersAccesRefuseSansCreerDeFacture()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var client = await CreerClientSansPermissionAsync();

        var reponse = await PosterGenererAsync(client, "/Compte/AccesRefuse");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/AccesRefuse", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
        Assert.False(await ExisteFactureAsync(idEchantillon));
    }

    [Fact]
    public async Task Generer_SansJetonAntiforgery_Retourne400SansCreerDeFacture()
    {
        var idEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var client = await CreerClientConnecteAsync();

        var reponse = await client.PostAsync("/Factures/Generer", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.False(await ExisteFactureAsync(idEchantillon));
    }

    [Fact]
    public async Task Generer_RequeteGet_Retourne404SansCreerDeFacture()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Generer");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_Administrateur_RetourneDuJsonPagine()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Resultats");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/json", reponse.Content.Headers.ContentType?.MediaType);
        var racine = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, racine.GetProperty("numeroPage").GetInt32());
        Assert.Equal(JsonValueKind.Array, racine.GetProperty("elements").ValueKind);
    }

    [Fact]
    public async Task Resultats_FactureEnBase_EstRestitueeAvecSonNomClient()
    {
        var nomClient = $"Client {Guid.NewGuid():N}";
        await AjouterFactureAsync(nomClient: nomClient, dateFacture: new DateOnly(2026, 3, 4));
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetStringAsync("/Factures/Resultats?dateFactureDebut=2026-03-04&dateFactureFin=2026-03-04&taillePage=200");

        Assert.Contains(nomClient, reponse, StringComparison.Ordinal);
        Assert.Contains("2026-03-04", reponse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resultats_NumeroPageMaximal_RetourneUnePageVideSansErreurServeur()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync($"/Factures/Resultats?numeroPage={int.MaxValue}&taillePage=200");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_TriInconnu_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Resultats?tri=Nimportequoi");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_DateInvalide_Retourne400()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Resultats?dateFactureDebut=pas-une-date");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Resultats_TriParMontant_RetourneLaPage()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Resultats?tri=MontantToutesTaxesComprises&triDescendant=false");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Imprimer_FactureExistante_RetourneLaPageAvecLeNumeroEtLeClient()
    {
        var nomClient = $"Client {Guid.NewGuid():N}";
        var identifiant = await AjouterFactureAsync(nomClient: nomClient);
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync($"/Factures/Imprimer/{identifiant}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains(nomClient, await reponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Imprimer_FactureInconnue_Retourne404()
    {
        var client = await CreerClientConnecteAsync();

        var reponse = await client.GetAsync("/Factures/Imprimer/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Theory]
    [InlineData("/Factures")]
    [InlineData("/Factures/Resultats")]
    [InlineData("/Factures/Imprimer/1")]
    public async Task Factures_Anonyme_RedirigeVersLaConnexion(string url)
    {
        var reponse = await CreerClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/Connexion", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Factures")]
    [InlineData("/Factures/Resultats")]
    [InlineData("/Factures/Imprimer/1")]
    public async Task Factures_CompteSansPermission_RedirigeVersAccesRefuse(string url)
    {
        var client = await CreerClientSansPermissionAsync();

        var reponse = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        Assert.Contains("/Compte/AccesRefuse", reponse.Headers.Location!.ToString(), StringComparison.Ordinal);
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

        var reponse = await client.GetAsync("/Factures/Resultats");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task Enregistrement_NumeroFactureDejaUtilise_EstRefuseParLIndexUnique()
    {
        var numero = NouveauNumero();
        await AjouterFactureAsync(numero: numero);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(numero: numero));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("IX_Factures_NumeroFacture", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enregistrement_DeuxFacturesPourUnEchantillon_EstRefuseParLIndexUnique()
    {
        var identifiantEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var identifiantClient = await ObtenirIdentifiantClientAsync(identifiantEchantillon);
        await AjouterFactureAsync(idEchantillon: identifiantEchantillon, idClient: identifiantClient);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AjouterFactureAsync(idEchantillon: identifiantEchantillon, idClient: identifiantClient));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("IX_Factures_IdEchantillon", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Suppression_ClientAvecFacture_EstRefuseParLaCleEtrangere()
    {
        var identifiantFacture = await AjouterFactureAsync();
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiantClient = await contexte.Factures.Where(f => f.Id == identifiantFacture).Select(f => f.IdClient).SingleAsync();

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => contexte.Clients.Where(c => c.Id == identifiantClient).ExecuteDeleteAsync());

        Assert.Contains("FK_Factures_Clients_IdClient", exception.Message, StringComparison.Ordinal);
        Assert.True(await contexte.Factures.AnyAsync(f => f.Id == identifiantFacture));
        Assert.True(await contexte.Clients.AnyAsync(c => c.Id == identifiantClient));
    }

    [Fact]
    public async Task Suppression_EchantillonAvecFacture_EstRefuseParLaCleEtrangere()
    {
        var identifiantFacture = await AjouterFactureAsync();
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiantEchantillon = await contexte.Factures.Where(f => f.Id == identifiantFacture).Select(f => f.IdEchantillon).SingleAsync();

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => contexte.Echantillons.Where(e => e.Id == identifiantEchantillon).ExecuteDeleteAsync());

        Assert.Contains("FK_Factures_Echantillons_IdEchantillon", exception.Message, StringComparison.Ordinal);
        Assert.True(await contexte.Factures.AnyAsync(f => f.Id == identifiantFacture));
        Assert.True(await contexte.Echantillons.AnyAsync(e => e.Id == identifiantEchantillon));
    }

    [Theory]
    [InlineData(-0.01, 0, 0)]
    [InlineData(0, -0.01, 0)]
    [InlineData(0, 0, -0.01)]
    public async Task Enregistrement_MontantNegatif_EstRefuseParLaContrainteCheck(double horsTaxe, double tva, double toutesTaxes)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(
            montantHorsTaxe: (decimal)horsTaxe,
            montantTva: (decimal)tva,
            montantToutesTaxes: (decimal)toutesTaxes));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CK_Factures_Montants", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enregistrement_MontantsNuls_EstAccepte()
    {
        var identifiant = await AjouterFactureAsync(montantHorsTaxe: 0m, montantTva: 0m, montantToutesTaxes: 0m);

        Assert.True(identifiant > 0);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task Enregistrement_RemiseHorsBornes_EstRefuseeParLaContrainteCheck(double remise)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(remise: (decimal)remise));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CK_Factures_Remise", erreurSql.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(12.5)]
    [InlineData(100.0)]
    public async Task Enregistrement_RemiseDansLesBornesInclusives_EstAcceptee(double remise)
    {
        var identifiant = await AjouterFactureAsync(remise: (decimal)remise);

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal((decimal)remise, (await contexte.Factures.SingleAsync(f => f.Id == identifiant)).Remise);
    }

    [Fact]
    public async Task Enregistrement_RemiseAbsente_EstAcceptee()
    {
        var identifiant = await AjouterFactureAsync(remise: null);

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Null((await contexte.Factures.SingleAsync(f => f.Id == identifiant)).Remise);
    }

    /// <summary>POST /Factures/Generer avec le jeton antiforgery lu sur une page (la liste par défaut).</summary>
    private static async Task<HttpResponseMessage> PosterGenererAsync(HttpClient client, string pageDuJeton = "/Factures")
    {
        var jeton = await client.ObtenirJetonAntiforgeryAsync(pageDuJeton);
        return await client.PostAsync("/Factures/Generer", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = jeton }));
    }

    private async Task<bool> ExisteFactureAsync(int idEchantillon)
    {
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        return await contexte.Factures.AnyAsync(facture => facture.IdEchantillon == idEchantillon);
    }

    private static string NouveauCode() => $"ECH-{Guid.NewGuid():N}";

    /// <summary>Numéro éloigné de la séquence réelle pour ne pas entrer en collision avec les factures générées par d'autres tests.</summary>
    private static int NouveauNumero() => Random.Shared.Next(100_000_000, 1_000_000_000);

    private async Task<int> AjouterEchantillonAvecClientAsync(string code)
    {
        using var portee = _fabrique.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = code,
            DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
            Filiere = "Elevage",
            StatutAnalyse = StatutsAnalyse.Recu,
            Client = new Client { RaisonSociale = $"Client {Guid.NewGuid():N}", Ville = "Lyon" },
        };
        contexte.Echantillons.Add(echantillon);
        await contexte.SaveChangesAsync();
        return echantillon.Id;
    }

    private async Task<int> ObtenirIdentifiantClientAsync(int idEchantillon)
    {
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        return await contexte.Echantillons.Where(e => e.Id == idEchantillon).Select(e => e.IdClient!.Value).SingleAsync();
    }

    /// <summary>Insère une facture (et, sauf indication contraire, son client et son échantillon) et renvoie son identifiant.</summary>
    private async Task<int> AjouterFactureAsync(
        int? numero = null,
        int? idEchantillon = null,
        int? idClient = null,
        string? nomClient = null,
        DateOnly? dateFacture = null,
        decimal montantHorsTaxe = 45m,
        decimal montantTva = 9m,
        decimal montantToutesTaxes = 54m,
        decimal? remise = null)
    {
        using var portee = _fabrique.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var facture = new Facture
        {
            NumeroFacture = numero ?? NouveauNumero(),
            DateFacture = dateFacture ?? new DateOnly(2026, 3, 4),
            DateEcheance = (dateFacture ?? new DateOnly(2026, 3, 4)).AddDays(30),
            NomClient = nomClient ?? "Acier SA",
            MontantHorsTaxe = montantHorsTaxe,
            TauxTva = 20m,
            MontantTva = montantTva,
            MontantToutesTaxesComprises = montantToutesTaxes,
            Remise = remise,
            Devise = "EUR",
            StatutFacture = StatutsFacture.Emise,
        };

        if (idClient is { } identifiantClient)
        {
            facture.IdClient = identifiantClient;
        }
        else
        {
            facture.Client = new Client { RaisonSociale = nomClient ?? "Acier SA" };
        }

        if (idEchantillon is { } identifiantEchantillon)
        {
            facture.IdEchantillon = identifiantEchantillon;
        }
        else
        {
            facture.Echantillon = new Echantillon
            {
                CodeBarresAnonyme = NouveauCode(),
                DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
                Filiere = "Elevage",
                StatutAnalyse = StatutsAnalyse.Recu,
            };
        }

        contexte.Factures.Add(facture);
        await contexte.SaveChangesAsync();
        return facture.Id;
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
