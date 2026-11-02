using erpWeb.Core.Clients;
using erpWeb.Core.DevisClients;
using erpWeb.Infrastructure.Donnees;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class GenerateurDevisDemonstrationTests : IDisposable
{
    private const int Quantite = 25;

    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task GenererAsync_AvecClients_GenereLaQuantiteDemandeeAvecDesLignes()
    {
        await AjouterClientsAsync(4);

        await CreerGenerateur().GenererAsync(Quantite);

        Assert.Equal(Quantite, await _base.Contexte.Devis.CountAsync());
        Assert.All(await _base.Contexte.Devis.Include(devis => devis.Lignes).ToListAsync(), devis => Assert.NotEmpty(devis.Lignes));
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesNumerosSontUniques()
    {
        await AjouterClientsAsync(4);

        await CreerGenerateur().GenererAsync(Quantite);

        var numeros = await _base.Contexte.Devis.Select(devis => devis.Numero).ToListAsync();
        Assert.Equal(numeros.Count, numeros.Distinct().Count());
    }

    [Fact]
    public async Task GenererAsync_AvecClients_LesTotauxSontCoherentsAvecLesLignes()
    {
        await AjouterClientsAsync(4);

        await CreerGenerateur().GenererAsync(Quantite);

        var tous = await _base.Contexte.Devis.Include(devis => devis.Lignes).ToListAsync();
        Assert.All(tous, devis => Assert.Equal(
            devis.MontantTotalHorsTaxe + devis.MontantTotalTaxe,
            devis.MontantTotalToutesTaxesComprises));
    }

    [Fact]
    public async Task GenererAsync_SansClient_NeGenereRien()
    {
        await CreerGenerateur().GenererAsync(Quantite);

        Assert.False(await _base.Contexte.Devis.AnyAsync());
    }

    [Fact]
    public async Task GenererAsync_DevisDejaPresents_EstIdempotent()
    {
        await AjouterClientsAsync(4);
        var generateur = CreerGenerateur();
        await generateur.GenererAsync(Quantite);

        await generateur.GenererAsync(Quantite);

        Assert.Equal(Quantite, await _base.Contexte.Devis.CountAsync());
    }

    private GenerateurDevisDemonstration CreerGenerateur()
        => new(_base.Contexte, new CalculateurDevis(), _base.Horloge, NullLogger<GenerateurDevisDemonstration>.Instance);

    private async Task AjouterClientsAsync(int nombre)
    {
        for (var index = 1; index <= nombre; index++)
        {
            _base.Contexte.Clients.Add(new Client { RaisonSociale = $"Client {index}" });
        }

        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
    }
}
