using System.Globalization;
using erpWeb.Core.Audit;
using erpWeb.Core.Parametres;
using erpWeb.UnitTests.Outils;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.UnitTests.Infrastructure;

public sealed class IntercepteurAuditTests : IDisposable
{
    private readonly BaseDonneesTest _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task SaveChanges_NouvelleEntite_TraceLaCreationAvecLIdentifiantGenere()
    {
        var parametre = new Parametre { Cle = "Smtp:Hote", Valeur = "smtp.exemple.fr" };
        _base.Contexte.Parametres.Add(parametre);

        await _base.Contexte.SaveChangesAsync();

        var entree = await _base.Contexte.JournalAudit.SingleAsync();
        Assert.Equal(ActionAudit.Creation, entree.Action);
        Assert.Equal(nameof(Parametre), entree.TypeEntite);
        Assert.Equal(parametre.Id.ToString(CultureInfo.InvariantCulture), entree.IdEntite);
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, entree.Utilisateur);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, entree.Date);
        Assert.Contains("smtp.exemple.fr", entree.NouvellesValeurs);
    }

    [Fact]
    public async Task SaveChanges_NouvelleEntiteAuditable_RenseigneLesChampsDeCreation()
    {
        var parametre = new Parametre { Cle = "Stockage:Dossier", Valeur = "documents" };
        _base.Contexte.Parametres.Add(parametre);

        await _base.Contexte.SaveChangesAsync();

        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, parametre.DateCreation);
        Assert.Equal(BaseDonneesTest.NomUtilisateurTest, parametre.CreePar);
        Assert.Null(parametre.DateModification);
    }

    [Fact]
    public async Task SaveChanges_EntiteModifiee_TraceLesValeursAvantEtApres()
    {
        var parametre = new Parametre { Cle = "Smtp:Port", Valeur = "25" };
        _base.Contexte.Parametres.Add(parametre);
        await _base.Contexte.SaveChangesAsync();

        _base.Horloge.Advance(TimeSpan.FromHours(1));
        parametre.Valeur = "587";
        await _base.Contexte.SaveChangesAsync();

        var modification = await _base.Contexte.JournalAudit.SingleAsync(entree => entree.Action == ActionAudit.Modification);
        Assert.Contains("\"25\"", modification.AnciennesValeurs);
        Assert.Contains("\"587\"", modification.NouvellesValeurs);
        Assert.Equal(BaseDonneesTest.DateReference.AddHours(1).UtcDateTime, parametre.DateModification);
        Assert.Equal(BaseDonneesTest.DateReference.UtcDateTime, parametre.DateCreation);
    }

    [Fact]
    public async Task SaveChanges_EntiteSupprimee_TraceLaSuppressionAvecLesAnciennesValeurs()
    {
        var parametre = new Parametre { Cle = "Test:Suppression", Valeur = "a-supprimer" };
        _base.Contexte.Parametres.Add(parametre);
        await _base.Contexte.SaveChangesAsync();

        _base.Contexte.Parametres.Remove(parametre);
        await _base.Contexte.SaveChangesAsync();

        var suppression = await _base.Contexte.JournalAudit.SingleAsync(entree => entree.Action == ActionAudit.Suppression);
        Assert.Contains("a-supprimer", suppression.AnciennesValeurs);
        Assert.Null(suppression.NouvellesValeurs);
    }

    [Fact]
    public async Task SaveChanges_AucuneModificationReelle_NeTraceRien()
    {
        var parametre = new Parametre { Cle = "Test:Inchange", Valeur = "valeur" };
        _base.Contexte.Parametres.Add(parametre);
        await _base.Contexte.SaveChangesAsync();

        parametre.Valeur = "valeur";
        await _base.Contexte.SaveChangesAsync();

        Assert.Equal(1, await _base.Contexte.JournalAudit.CountAsync());
    }
}
