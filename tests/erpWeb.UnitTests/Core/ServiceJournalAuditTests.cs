using erpWeb.Core.Audit;
using erpWeb.UnitTests.Outils;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceJournalAuditTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task ObtenirActiviteAsync_EntreesSurPlusieursJours_RetourneUnComptageParJour()
    {
        var maintenant = BaseDonneesTest.DateReference.UtcDateTime;
        _base.Contexte.JournalAudit.AddRange(
            CreerEntree(maintenant),
            CreerEntree(maintenant.AddHours(-1)),
            CreerEntree(maintenant.AddDays(-2)),
            CreerEntree(maintenant.AddDays(-40)));
        await _base.Contexte.SaveChangesAsync();

        var activite = await CreerService().ObtenirActiviteAsync(30);

        Assert.Equal(30, activite.Count);
        Assert.Equal(DateOnly.FromDateTime(maintenant), activite[^1].Jour);
        Assert.Equal(2, activite[^1].NombreOperations);
        Assert.Equal(1, activite[^3].NombreOperations);
        Assert.Equal(3, activite.Sum(jour => jour.NombreOperations));
    }

    [Fact]
    public async Task ListerRecentesAsync_PlusDEntreesQueLeMaximum_RetourneLesPlusRecentesEnPremier()
    {
        var maintenant = BaseDonneesTest.DateReference.UtcDateTime;
        for (var decalage = 0; decalage < 5; decalage++)
        {
            _base.Contexte.JournalAudit.Add(CreerEntree(maintenant.AddMinutes(-decalage)));
        }

        await _base.Contexte.SaveChangesAsync();

        var entrees = await CreerService().ListerRecentesAsync(3);

        Assert.Equal(3, entrees.Count);
        Assert.Equal(maintenant, entrees[0].Date);
        Assert.True(entrees[0].Date > entrees[2].Date);
    }

    private ServiceJournalAudit CreerService() => new(_base.Contexte, BaseDonneesTest.CreerMapper(), _base.Horloge);

    private static EntreeJournalAudit CreerEntree(DateTime date) => new()
    {
        TypeEntite = "Parametre",
        IdEntite = "1",
        Action = ActionAudit.Modification,
        Utilisateur = BaseDonneesTest.NomUtilisateurTest,
        Date = date,
    };
}
