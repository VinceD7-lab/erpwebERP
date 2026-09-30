using erpWeb.Core.Clients;
using erpWeb.Infrastructure.Donnees;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class GenerateurClientsDemonstrationTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task GenererAsync_BaseVide_GenereLaQuantiteDemandee()
    {
        await CreerGenerateur().GenererAsync(quantite: 10);

        var clients = await _base.Contexte.Clients.ToListAsync();
        Assert.Equal(10, clients.Count);
        Assert.All(clients, client => Assert.False(string.IsNullOrWhiteSpace(client.RaisonSociale)));
    }

    [Fact]
    public async Task GenererAsync_ClientsDejaPresents_CompleteJusquALaCibleSansDupliquerLesExistants()
    {
        await AjouterClientAsync("Acier SA");
        await AjouterClientAsync("Zinc SARL");

        await CreerGenerateur().GenererAsync(quantite: 5);

        var raisonsSociales = (await _base.Contexte.Clients.ToListAsync()).Select(client => client.RaisonSociale).ToList();
        Assert.Equal(5, raisonsSociales.Count);
        Assert.Contains("Acier SA", raisonsSociales);
        Assert.Contains("Zinc SARL", raisonsSociales);
    }

    [Fact]
    public async Task GenererAsync_DejaAuMoinsLaQuantiteCible_NeGenereRien()
    {
        await AjouterClientAsync("Acier SA");
        await AjouterClientAsync("Zinc SARL");

        await CreerGenerateur().GenererAsync(quantite: 2);

        Assert.Equal(2, await _base.Contexte.Clients.CountAsync());
    }

    [Fact]
    public async Task GenererAsync_AppelsSuccessifs_NeDupliquePasLesClientsExistants()
    {
        var generateur = CreerGenerateur();

        await generateur.GenererAsync(quantite: 5);
        await generateur.GenererAsync(quantite: 5);

        Assert.Equal(5, await _base.Contexte.Clients.CountAsync());
    }

    private GenerateurClientsDemonstration CreerGenerateur()
        => new(
            _base.Contexte,
            new ServiceClients(_base.Contexte, new ValidateurCreationClient(), new ValidateurModificationClient(), BaseDonneesTest.CreerMapper()),
            NullLogger<GenerateurClientsDemonstration>.Instance);

    private async Task AjouterClientAsync(string raisonSociale)
    {
        _base.Contexte.Clients.Add(new Client { RaisonSociale = raisonSociale });
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
    }
}
