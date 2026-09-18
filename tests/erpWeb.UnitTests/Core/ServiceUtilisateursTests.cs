using Bogus;
using erpWeb.Core;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWeb.Core.Email;
using erpWeb.Core.Parametres;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure.Donnees;
using erpWeb.UnitTests.Outils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace erpWeb.UnitTests.Core;

/// <summary>
/// ServiceUtilisateurs s'appuie sur UserManager/RoleManager : les tests utilisent l'implémentation
/// réelle d'Identity sur SQLite en mémoire plutôt qu'un mock de UserManager.
/// </summary>
public sealed class ServiceUtilisateursTests : IAsyncLifetime
{
    private const string MotDePasseValide = "MotDePasse#2026";

    private readonly SqliteConnection _connexion = new("DataSource=:memory:");
    private readonly Mock<IServiceEmail> _serviceEmail = new();
    private readonly Faker _faker = new("fr");
    private ServiceProvider _fournisseur = default!;
    private AsyncServiceScope _portee;

    private IServiceUtilisateurs Service => _portee.ServiceProvider.GetRequiredService<IServiceUtilisateurs>();

    private UserManager<Utilisateur> GestionnaireUtilisateurs => _portee.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();

    public async Task InitializeAsync()
    {
        await _connexion.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(BaseDonneesTest.DateReference));
        services.AddSingleton(Mock.Of<IUtilisateurCourant>());
        services.AddSingleton(Mock.Of<IRechargementConfiguration>());
        services.AddSingleton(Mock.Of<IStockageFichiers>());
        services.AddSingleton(_serviceEmail.Object);
        services.AddScoped<IntercepteurAudit>();
        services.AddDbContext<AppDbContext>((fournisseur, options) => options
            .UseSqlite(_connexion)
            .AddInterceptors(fournisseur.GetRequiredService<IntercepteurAudit>()));
        services.AddScoped<IAppDbContext>(fournisseur => fournisseur.GetRequiredService<AppDbContext>());
        services.AddIdentityCore<Utilisateur>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddCore();

        _fournisseur = services.BuildServiceProvider();
        _portee = _fournisseur.CreateAsyncScope();

        await _portee.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        var gestionnaireRoles = _portee.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await gestionnaireRoles.CreateAsync(new IdentityRole(RolesApplication.Administrateur));
        await gestionnaireRoles.CreateAsync(new IdentityRole(RolesApplication.Utilisateur));
    }

    public async Task DisposeAsync()
    {
        await _portee.DisposeAsync();
        await _fournisseur.DisposeAsync();
        await _connexion.DisposeAsync();
    }

    [Fact]
    public async Task CreerAsync_DonneesValides_CreeLeCompteAvecSesRolesEtEnvoieUnEmail()
    {
        var creation = CreerDemande(RolesApplication.Administrateur);

        var resultat = await Service.CreerAsync(creation);

        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        var utilisateur = await Service.ObtenirAsync(resultat.Valeur!);
        Assert.NotNull(utilisateur);
        Assert.Equal(creation.Email, utilisateur.Email);
        Assert.True(utilisateur.EstActif);
        Assert.Equal([RolesApplication.Administrateur], utilisateur.Roles);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, utilisateur.DateCreation);
        _serviceEmail.Verify(
            service => service.EnvoyerAsync(It.Is<MessageEmail>(message => message.Destinataire == creation.Email), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreerAsync_EmailInvalide_RetourneEchecSansEnvoyerDEmail()
    {
        var creation = CreerDemande(RolesApplication.Utilisateur);
        creation.Email = "adresse-invalide";

        var resultat = await Service.CreerAsync(creation);

        Assert.False(resultat.Reussi);
        Assert.Empty(await Service.ListerAsync());
        _serviceEmail.Verify(service => service.EnvoyerAsync(It.IsAny<MessageEmail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreerAsync_RoleInconnu_RetourneEchecSansCreerLeCompte()
    {
        var resultat = await Service.CreerAsync(CreerDemande("RoleInexistant"));

        Assert.False(resultat.Reussi);
        Assert.Contains(resultat.Erreurs, erreur => erreur.Contains("RoleInexistant", StringComparison.Ordinal));
        Assert.Empty(await Service.ListerAsync());
    }

    [Fact]
    public async Task InscrireAsync_DonneesValides_AttribueLeRoleUtilisateur()
    {
        var inscription = new InscriptionDto
        {
            Email = CreerEmail(),
            NomComplet = _faker.Name.FullName(),
            MotDePasse = MotDePasseValide,
            ConfirmationMotDePasse = MotDePasseValide,
        };

        var resultat = await Service.InscrireAsync(inscription);

        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        var utilisateur = await Service.ObtenirAsync(resultat.Valeur!);
        Assert.Equal([RolesApplication.Utilisateur], utilisateur!.Roles);
    }

    [Fact]
    public async Task DefinirActivationAsync_Desactivation_RendLeCompteInactif()
    {
        var identifiant = (await Service.CreerAsync(CreerDemande(RolesApplication.Utilisateur))).Valeur!;

        var resultat = await Service.DefinirActivationAsync(identifiant, estActif: false);

        Assert.True(resultat.Reussi);
        Assert.False((await Service.ObtenirAsync(identifiant))!.EstActif);
    }

    [Fact]
    public async Task ModifierAsync_NouveauxRoles_RemplaceLesRolesExistants()
    {
        var creation = CreerDemande(RolesApplication.Utilisateur);
        var identifiant = (await Service.CreerAsync(creation)).Valeur!;

        var resultat = await Service.ModifierAsync(new ModificationUtilisateurDto
        {
            Id = identifiant,
            Email = creation.Email,
            NomComplet = "Nom modifié",
            Roles = [RolesApplication.Administrateur],
        });

        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        var utilisateur = await Service.ObtenirAsync(identifiant);
        Assert.Equal("Nom modifié", utilisateur!.NomComplet);
        Assert.Equal([RolesApplication.Administrateur], utilisateur.Roles);
    }

    [Fact]
    public async Task ReinitialiserMotDePasseAsync_NouveauMotDePasse_RemplaceLAncien()
    {
        var identifiant = (await Service.CreerAsync(CreerDemande(RolesApplication.Utilisateur))).Valeur!;
        const string nouveauMotDePasse = "Nouveau#Secret2026";

        var resultat = await Service.ReinitialiserMotDePasseAsync(new ReinitialisationMotDePasseDto { Id = identifiant, NouveauMotDePasse = nouveauMotDePasse });

        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        var utilisateur = await GestionnaireUtilisateurs.FindByIdAsync(identifiant);
        Assert.True(await GestionnaireUtilisateurs.CheckPasswordAsync(utilisateur!, nouveauMotDePasse));
        Assert.False(await GestionnaireUtilisateurs.CheckPasswordAsync(utilisateur!, MotDePasseValide));
    }

    private CreationUtilisateurDto CreerDemande(string role) => new()
    {
        Email = CreerEmail(),
        NomComplet = _faker.Name.FullName(),
        MotDePasse = MotDePasseValide,
        Roles = [role],
    };

    private string CreerEmail() => $"{_faker.Random.AlphaNumeric(12)}@erpweb.local";
}
