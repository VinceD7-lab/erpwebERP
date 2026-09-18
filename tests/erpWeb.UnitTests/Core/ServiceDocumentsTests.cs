using Bogus;
using erpWeb.Core.Documents;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceDocumentsTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();
    private readonly Mock<IStockageFichiers> _stockage = new();
    private readonly Faker _faker = new("fr");

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task DeposerAsync_DepotValide_EnregistreLeFichierEtLeDocument()
    {
        var nomFichier = _faker.System.FileName("pdf");
        _stockage
            .Setup(stockage => stockage.EnregistrerAsync(It.IsAny<Stream>(), nomFichier, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ab/abcdef.pdf");
        using var contenu = new MemoryStream([1, 2, 3]);

        var resultat = await CreerService().DeposerAsync(CreerDepot(nomFichier, contenu, taille: 3));

        Assert.True(resultat.Reussi);
        var document = await _base.Contexte.Documents.SingleAsync();
        Assert.Equal(resultat.Valeur, document.Id);
        Assert.Equal("ab/abcdef.pdf", document.CheminStockage);
        Assert.Equal(nomFichier, document.NomFichier);
    }

    [Fact]
    public async Task DeposerAsync_FichierVide_RetourneEchecSansStockerLeFichier()
    {
        using var contenu = new MemoryStream();

        var resultat = await CreerService().DeposerAsync(CreerDepot("vide.txt", contenu, taille: 0));

        Assert.False(resultat.Reussi);
        Assert.Contains("Le fichier est vide.", resultat.Erreurs);
        _stockage.Verify(
            stockage => stockage.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SupprimerAsync_DocumentExistant_SupprimeEnBaseEtDansLeStockage()
    {
        var document = new Document { TypeEntite = "Client", IdEntite = "42", NomFichier = "contrat.pdf", TypeContenu = "application/pdf", Taille = 10, CheminStockage = "cd/contrat.pdf" };
        _base.Contexte.Documents.Add(document);
        await _base.Contexte.SaveChangesAsync();

        var resultat = await CreerService().SupprimerAsync(document.Id);

        Assert.True(resultat.Reussi);
        Assert.False(await _base.Contexte.Documents.AnyAsync());
        _stockage.Verify(stockage => stockage.SupprimerAsync("cd/contrat.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SupprimerAsync_DocumentInexistant_RetourneEchec()
    {
        var resultat = await CreerService().SupprimerAsync(999);

        Assert.False(resultat.Reussi);
    }

    private ServiceDocuments CreerService()
        => new(_base.Contexte, _stockage.Object, new ValidateurDepotDocument(Options.Create(new OptionsStockage())), BaseDonneesTest.CreerMapper());

    private static DepotDocumentDto CreerDepot(string nomFichier, Stream contenu, long taille) => new()
    {
        TypeEntite = "Client",
        IdEntite = "42",
        NomFichier = nomFichier,
        TypeContenu = "application/pdf",
        Taille = taille,
        Contenu = contenu,
    };
}
