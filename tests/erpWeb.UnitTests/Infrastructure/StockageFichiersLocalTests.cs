using erpWeb.Core.Documents;
using erpWeb.Infrastructure.Stockage;
using Microsoft.Extensions.Options;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class StockageFichiersLocalTests : IDisposable
{
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "erpWeb-stockage-" + Guid.NewGuid().ToString("N"));
    private readonly StockageFichiersLocal _stockage;

    public StockageFichiersLocalTests()
    {
        _stockage = new StockageFichiersLocal(Options.Create(new OptionsStockage { Dossier = _dossier }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    [Fact]
    public async Task EnregistrerAsync_PuisOuvrirAsync_RestitueLeContenu()
    {
        byte[] octets = [10, 20, 30, 40];

        var chemin = await _stockage.EnregistrerAsync(new MemoryStream(octets), "Facture Mars.PDF");
        await using var flux = await _stockage.OuvrirAsync(chemin);
        using var copie = new MemoryStream();
        await flux.CopyToAsync(copie);

        Assert.Equal(octets, copie.ToArray());
        Assert.EndsWith(".pdf", chemin, StringComparison.Ordinal);
        Assert.DoesNotContain("Facture", chemin, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnregistrerAsync_NomAvecTraverseeDeRepertoire_EnregistreDansLeDossierAutorise()
    {
        var chemin = await _stockage.EnregistrerAsync(new MemoryStream([1]), "../../../malveillant.exe");

        Assert.DoesNotContain("..", chemin, StringComparison.Ordinal);
        var fichiers = Directory.GetFiles(_dossier, "*", SearchOption.AllDirectories);
        Assert.Single(fichiers);
    }

    [Fact]
    public async Task OuvrirAsync_CheminHorsDuDossier_LeveUneException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _stockage.OuvrirAsync("../../secret.txt"));
    }

    [Fact]
    public async Task SupprimerAsync_FichierExistant_SupprimeLeFichier()
    {
        var chemin = await _stockage.EnregistrerAsync(new MemoryStream([1, 2]), "note.txt");

        await _stockage.SupprimerAsync(chemin);

        Assert.Empty(Directory.GetFiles(_dossier, "*", SearchOption.AllDirectories));
    }
}
