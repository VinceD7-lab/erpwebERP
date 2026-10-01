using Bogus;
using erpWeb.Core.Clients;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceClientsTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();
    private readonly Faker _faker = new("fr");

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task CreerAsync_ClientValide_EnregistreLeClientEtRenseigneLAudit()
    {
        var creation = CreerSaisieValide();

        var resultat = await CreerService().CreerAsync(creation);

        Assert.True(resultat.Reussi);
        var client = await _base.Contexte.Clients.SingleAsync();
        Assert.Equal(resultat.Valeur, client.Id);
        Assert.Equal(creation.RaisonSociale, client.RaisonSociale);
        Assert.Equal(creation.Email, client.Email);
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, client.CreePar);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, client.DateCreation);
    }

    [Fact]
    public async Task CreerAsync_ChampsFacultatifsVides_EnregistreLeClient()
    {
        var resultat = await CreerService().CreerAsync(new CreationClientDto { RaisonSociale = "Société minimale" });

        Assert.True(resultat.Reussi);
        Assert.Null((await _base.Contexte.Clients.SingleAsync()).CodePostal);
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("Société", "7500", null)]
    [InlineData("Société", "75A01", null)]
    [InlineData("Société", null, "adresse-invalide")]
    public async Task CreerAsync_SaisieInvalide_RetourneEchecSansEnregistrer(string raisonSociale, string? codePostal, string? email)
    {
        var creation = new CreationClientDto { RaisonSociale = raisonSociale, CodePostal = codePostal, Email = email };

        var resultat = await CreerService().CreerAsync(creation);

        Assert.False(resultat.Reussi);
        Assert.NotEmpty(resultat.Erreurs);
        Assert.False(await _base.Contexte.Clients.AnyAsync());
    }

    [Fact]
    public async Task CreerAsync_TelephonesAvecEspacesEtPoints_EnregistreLesNumerosSansSeparateurs()
    {
        var creation = new CreationClientDto { RaisonSociale = "Acier SA", Telephone1 = "01 02.03 04 05", Telephone2 = "+33 6.12.34.56.78" };

        var resultat = await CreerService().CreerAsync(creation);

        Assert.True(resultat.Reussi);
        var client = await _base.Contexte.Clients.SingleAsync();
        Assert.Equal("0102030405", client.Telephone1);
        Assert.Equal("+33612345678", client.Telephone2);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("01-02-03-04-05")]
    [InlineData("01 02 03 04 05 06 07")]
    public async Task CreerAsync_TelephoneInvalide_RetourneEchec(string telephone)
    {
        var resultat = await CreerService().CreerAsync(new CreationClientDto { RaisonSociale = "Acier SA", Telephone1 = telephone });

        Assert.False(resultat.Reussi);
    }

    [Theory]
    [InlineData("Royaume-Uni", "SW1A 1AA")]
    [InlineData("Belgique", "1000")]
    [InlineData("Canada", "K1A 0B1")]
    [InlineData("france", "69001")]
    public async Task CreerAsync_CodePostalConformeAuPays_EnregistreLeClient(string pays, string codePostal)
    {
        var resultat = await CreerService().CreerAsync(new CreationClientDto { RaisonSociale = "Acier SA", Pays = pays, CodePostal = codePostal });

        Assert.True(resultat.Reussi);
    }

    [Theory]
    [InlineData("France", "1000")]
    [InlineData("France", "75001\n")]
    [InlineData("Belgique", "1000\n")]
    [InlineData("Belgique", "10@0")]
    [InlineData("Royaume-Uni", "SW1A 1AA XYZ")]
    public async Task CreerAsync_CodePostalNonConformeAuPays_RetourneEchec(string pays, string codePostal)
    {
        var resultat = await CreerService().CreerAsync(new CreationClientDto { RaisonSociale = "Acier SA", Pays = pays, CodePostal = codePostal });

        Assert.False(resultat.Reussi);
    }

    [Fact]
    public async Task ModifierAsync_TelephoneAvecSeparateurs_EnregistreLeNumeroSansSeparateurs()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().ModifierAsync(new ModificationClientDto { Id = client.Id, RaisonSociale = "Acier SA", Telephone1 = "04.72.00.00.00" });

        Assert.True(resultat.Reussi);
        Assert.Equal("0472000000", (await _base.Contexte.Clients.AsNoTracking().SingleAsync()).Telephone1);
    }

    [Fact]
    public async Task CreerAsync_RaisonSocialeTropLongue_RetourneEchec()
    {
        var creation = new CreationClientDto { RaisonSociale = new string('a', LongueursClient.RaisonSociale + 1) };

        var resultat = await CreerService().CreerAsync(creation);

        Assert.False(resultat.Reussi);
    }

    [Fact]
    public async Task ListerAsync_PlusieursClients_RetourneLesClientsTriesParRaisonSociale()
    {
        await AjouterClientAsync("Zinc SARL");
        await AjouterClientAsync("Acier SA");

        var clients = await CreerService().ListerAsync();

        Assert.Equal(["Acier SA", "Zinc SARL"], clients.Select(client => client.RaisonSociale));
    }

    [Fact]
    public async Task ObtenirAsync_ClientExistant_RetourneLeClient()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().ObtenirAsync(client.Id);

        Assert.NotNull(resultat);
        Assert.Equal(client.Id, resultat.Id);
        Assert.Equal("Acier SA", resultat.RaisonSociale);
    }

    [Fact]
    public async Task ObtenirAsync_ClientInexistant_RetourneNull()
    {
        Assert.Null(await CreerService().ObtenirAsync(999));
    }

    [Fact]
    public async Task ObtenirPourModificationAsync_ClientExistant_RetourneLaSaisiePreremplie()
    {
        var client = await AjouterClientAsync("Acier SA");

        var modification = await CreerService().ObtenirPourModificationAsync(client.Id);

        Assert.NotNull(modification);
        Assert.Equal(client.Id, modification.Id);
        Assert.Equal("Acier SA", modification.RaisonSociale);
    }

    [Fact]
    public async Task ObtenirPourModificationAsync_ClientInexistant_RetourneNull()
    {
        Assert.Null(await CreerService().ObtenirPourModificationAsync(999));
    }

    [Fact]
    public async Task ModifierAsync_ClientExistant_EnregistreLesModificationsEtLAudit()
    {
        var client = await AjouterClientAsync("Acier SA");
        _base.Horloge.Advance(TimeSpan.FromHours(1));
        var modification = new ModificationClientDto { Id = client.Id, RaisonSociale = "Acier et Fils", Ville = "Lyon", CodePostal = "69001" };

        var resultat = await CreerService().ModifierAsync(modification);

        Assert.True(resultat.Reussi);
        var enregistre = await _base.Contexte.Clients.AsNoTracking().SingleAsync();
        Assert.Equal(client.Id, enregistre.Id);
        Assert.Equal("Acier et Fils", enregistre.RaisonSociale);
        Assert.Equal("Lyon", enregistre.Ville);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, enregistre.DateCreation);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime.AddHours(1), enregistre.DateModification);
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, enregistre.ModifiePar);
    }

    [Fact]
    public async Task ModifierAsync_ChampFacultatifVide_EffaceLaValeur()
    {
        var client = new Client { RaisonSociale = "Acier SA", Ville = "Lyon" };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();

        var resultat = await CreerService().ModifierAsync(new ModificationClientDto { Id = client.Id, RaisonSociale = "Acier SA", Ville = null });

        Assert.True(resultat.Reussi);
        Assert.Null((await _base.Contexte.Clients.AsNoTracking().SingleAsync()).Ville);
    }

    [Fact]
    public async Task ModifierAsync_IdentifiantInvalide_RetourneEchec()
    {
        var resultat = await CreerService().ModifierAsync(new ModificationClientDto { Id = 0, RaisonSociale = "Acier SA" });

        Assert.False(resultat.Reussi);
    }

    [Theory]
    [InlineData(nameof(CreationClientDto.Adresse1), LongueursClient.Adresse)]
    [InlineData(nameof(CreationClientDto.Ville), LongueursClient.Ville)]
    [InlineData(nameof(CreationClientDto.Pays), LongueursClient.Pays)]
    public async Task CreerAsync_ChampFacultatifTropLong_RetourneEchec(string propriete, int longueurMaximale)
    {
        var creation = new CreationClientDto { RaisonSociale = "Acier SA" };
        typeof(CreationClientDto).GetProperty(propriete)!.SetValue(creation, new string('a', longueurMaximale + 1));

        var resultat = await CreerService().CreerAsync(creation);

        Assert.False(resultat.Reussi);
    }

    [Fact]
    public async Task ModifierAsync_ClientInexistant_RetourneEchec()
    {
        var resultat = await CreerService().ModifierAsync(new ModificationClientDto { Id = 999, RaisonSociale = "Fantôme" });

        Assert.False(resultat.Reussi);
        Assert.Contains("Client introuvable.", resultat.Erreurs);
    }

    [Fact]
    public async Task ModifierAsync_SaisieInvalide_RetourneEchecSansModifier()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().ModifierAsync(new ModificationClientDto { Id = client.Id, RaisonSociale = string.Empty });

        Assert.False(resultat.Reussi);
        Assert.Equal("Acier SA", (await _base.Contexte.Clients.AsNoTracking().SingleAsync()).RaisonSociale);
    }

    [Fact]
    public async Task SupprimerAsync_ClientExistant_SupprimeLeClient()
    {
        var client = await AjouterClientAsync("Acier SA");

        var resultat = await CreerService().SupprimerAsync(client.Id);

        Assert.True(resultat.Reussi);
        Assert.False(await _base.Contexte.Clients.AnyAsync());
    }

    [Fact]
    public async Task SupprimerAsync_ClientInexistant_RetourneEchec()
    {
        var resultat = await CreerService().SupprimerAsync(999);

        Assert.False(resultat.Reussi);
    }

    private ServiceClients CreerService()
        => new(_base.Contexte, new ValidateurCreationClient(), new ValidateurModificationClient(), BaseDonneesTest.CreerMapper());

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return client;
    }

    private CreationClientDto CreerSaisieValide() => new()
    {
        RaisonSociale = _faker.Company.CompanyName(),
        Adresse1 = _faker.Address.StreetAddress(),
        CodePostal = "75001",
        Ville = _faker.Address.City(),
        Pays = "France",
        Telephone1 = "0102030405",
        Email = $"contact-{_faker.Random.AlphaNumeric(8)}@erpweb.local",
    };
}
