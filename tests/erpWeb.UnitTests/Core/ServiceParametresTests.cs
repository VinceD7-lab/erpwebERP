using erpWeb.Core.Parametres;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceParametresTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();
    private readonly Mock<IRechargementConfiguration> _rechargement = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task DefinirAsync_NouvelleCle_CreeLeParametreEtRechargeLaConfiguration()
    {
        var resultat = await CreerService().DefinirAsync(new ModificationParametreDto { Cle = "Smtp:Hote", Valeur = "smtp.exemple.fr" });

        Assert.True(resultat.Reussi);
        var parametre = await _base.Contexte.Parametres.SingleAsync();
        Assert.Equal("smtp.exemple.fr", parametre.Valeur);
        _rechargement.Verify(rechargement => rechargement.Recharger(), Times.Once);
    }

    [Fact]
    public async Task DefinirAsync_CleExistante_MetAJourLaValeur()
    {
        var service = CreerService();
        await service.DefinirAsync(new ModificationParametreDto { Cle = "Smtp:Port", Valeur = "25" });

        await service.DefinirAsync(new ModificationParametreDto { Cle = "Smtp:Port", Valeur = "587" });

        var parametre = await _base.Contexte.Parametres.SingleAsync();
        Assert.Equal("587", parametre.Valeur);
    }

    [Theory]
    [InlineData("cle invalide")]
    [InlineData("Section::Cle")]
    [InlineData("")]
    public async Task DefinirAsync_CleInvalide_RetourneEchecSansRecharger(string cle)
    {
        var resultat = await CreerService().DefinirAsync(new ModificationParametreDto { Cle = cle, Valeur = "valeur" });

        Assert.False(resultat.Reussi);
        Assert.False(await _base.Contexte.Parametres.AnyAsync());
        _rechargement.Verify(rechargement => rechargement.Recharger(), Times.Never);
    }

    private ServiceParametres CreerService()
        => new(_base.Contexte, new ValidateurModificationParametre(), _rechargement.Object, BaseDonneesTest.CreerMapper());
}
