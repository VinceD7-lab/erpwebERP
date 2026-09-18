using erpWeb.Core;
using erpWeb.Core.Audit;
using erpWeb.Core.Documents;
using erpWeb.Core.TableauDeBord;
using erpWeb.Core.TableauDeBord.Widgets;
using erpWeb.Core.Utilisateurs;
using erpWeb.UnitTests.Outils;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace erpWeb.UnitTests.Core;

public sealed class WidgetsTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public void AddCore_EnregistreAutomatiquementTousLesWidgets()
    {
        var services = new ServiceCollection().AddCore();

        var typesWidgets = services
            .Where(descripteur => descripteur.ServiceType == typeof(IWidgetTableauDeBord))
            .Select(descripteur => descripteur.ImplementationType)
            .ToList();

        Assert.Equal(4, typesWidgets.Count);
        Assert.Contains(typeof(WidgetActiviteAudit), typesWidgets);
    }

    [Fact]
    public async Task WidgetUtilisateursActifs_ComptesActifsEtInactifs_AfficheLeNombreDeComptesActifs()
    {
        _base.Contexte.Users.AddRange(
            CreerUtilisateur("a@erpweb.local", estActif: true),
            CreerUtilisateur("b@erpweb.local", estActif: true),
            CreerUtilisateur("c@erpweb.local", estActif: false));
        await _base.Contexte.SaveChangesAsync();

        var donnees = await new WidgetUtilisateursActifs(_base.Contexte).ObtenirDonneesAsync();

        var indicateur = Assert.IsType<DonneesIndicateur>(donnees);
        Assert.Equal("2", indicateur.Valeur);
        Assert.Contains("3", indicateur.Detail);
    }

    [Fact]
    public async Task WidgetActiviteAudit_ActiviteSurTrenteJours_RetourneUnGraphiqueDeTrentePoints()
    {
        var premierJour = new DateOnly(2026, 8, 17);
        var activite = Enumerable.Range(0, WidgetActiviteAudit.NombreJours)
            .Select(decalage => new ActiviteJournaliere(premierJour.AddDays(decalage), decalage % 3))
            .ToList();
        var lecture = new Mock<ILectureJournalAudit>();
        lecture.Setup(l => l.ObtenirActiviteAsync(WidgetActiviteAudit.NombreJours, It.IsAny<CancellationToken>())).ReturnsAsync(activite);

        var donnees = await new WidgetActiviteAudit(lecture.Object).ObtenirDonneesAsync();

        var graphique = Assert.IsType<DonneesGraphique>(donnees);
        Assert.Equal(WidgetActiviteAudit.NombreJours, graphique.Etiquettes.Count);
        Assert.Equal("17/08", graphique.Etiquettes[0]);
        Assert.Equal(activite.Sum(jour => jour.NombreOperations), Assert.Single(graphique.Series).Valeurs.Sum());
    }

    [Fact]
    public async Task WidgetDerniersDocuments_DocumentsRecents_RetourneUneListeAvecLiensDeTelechargement()
    {
        var service = new Mock<IServiceDocuments>();
        service.Setup(s => s.ListerRecentsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new DocumentDto { Id = 7, NomFichier = "devis.pdf", TypeEntite = "Client", IdEntite = "12", DateCreation = BaseDonneesTest.DateReference.UtcDateTime },
        ]);

        var donnees = await new WidgetDerniersDocuments(service.Object).ObtenirDonneesAsync();

        var liste = Assert.IsType<DonneesListe>(donnees);
        var element = Assert.Single(liste.Elements);
        Assert.Equal("devis.pdf", element.Libelle);
        Assert.Equal("/Documents/Telecharger/7", element.Url);
    }

    [Fact]
    public async Task WidgetRaccourcisAdministration_RetourneDesRaccourcisProtegesParPermission()
    {
        var donnees = await new WidgetRaccourcisAdministration().ObtenirDonneesAsync();

        var raccourcis = Assert.IsType<DonneesRaccourcis>(donnees);
        Assert.Equal(4, raccourcis.Raccourcis.Count);
        Assert.All(raccourcis.Raccourcis, raccourci => Assert.NotNull(raccourci.PermissionRequise));
    }

    private static Utilisateur CreerUtilisateur(string email, bool estActif) => new()
    {
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        NomComplet = email,
        EstActif = estActif,
    };
}
