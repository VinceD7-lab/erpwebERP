using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.UnitTests.Outils;
using Microsoft.Extensions.Options;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceImpressionFactureTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task ObtenirAsync_FactureExistante_RetourneLaFactureEtLIdentiteDeLEmetteur()
    {
        var client = new Client { RaisonSociale = "Acier SA" };
        var facture = new Facture
        {
            Echantillon = new Echantillon
            {
                CodeBarresAnonyme = "ECH-00001",
                DatePrelevement = BaseDonneesTest.DateReference.UtcDateTime,
                Filiere = "Elevage",
                Client = client,
            },
            Client = client,
            NumeroFacture = 12,
            DateFacture = new DateOnly(2026, 9, 10),
            NomClient = "Acier SA",
            MontantHorsTaxe = 38m,
            TauxTva = 20m,
            MontantTva = 7.60m,
            MontantToutesTaxesComprises = 45.60m,
            Devise = "EUR",
        };
        _base.Contexte.Factures.Add(facture);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        var options = new OptionsFacturation { NomEmetteur = "Laboratoire Test", AdresseEmetteur = "1 rue du Labo, 69000 Lyon" };

        var impression = await CreerService(options).ObtenirAsync(facture.Id);

        Assert.NotNull(impression);
        Assert.Equal(12, impression.Facture.NumeroFacture);
        Assert.Equal("ECH-00001", impression.Facture.CodeBarresAnonyme);
        Assert.Equal("Elevage", impression.Facture.Filiere);
        Assert.Equal(45.60m, impression.Facture.MontantToutesTaxesComprises);
        Assert.Equal("Laboratoire Test", impression.NomEmetteur);
        Assert.Equal("1 rue du Labo, 69000 Lyon", impression.AdresseEmetteur);
    }

    [Fact]
    public async Task ObtenirAsync_FactureInconnue_RetourneNull()
    {
        var impression = await CreerService(new OptionsFacturation()).ObtenirAsync(9_999);

        Assert.Null(impression);
    }

    private ServiceImpressionFacture CreerService(OptionsFacturation options) => new(_base.Contexte, Options.Create(options));
}
