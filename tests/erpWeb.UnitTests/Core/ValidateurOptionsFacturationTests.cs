using erpWeb.Core.Factures;

namespace erpWeb.UnitTests.Core;

public sealed class ValidateurOptionsFacturationTests
{
    private readonly ValidateurOptionsFacturation _validateur = new();

    [Fact]
    public void Validate_ValeursParDefaut_Reussit()
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation());

        Assert.True(resultat.Succeeded);
    }

    [Fact]
    public void Validate_BornesInclusives_Reussit()
    {
        var options = new OptionsFacturation
        {
            TauxTva = 100m,
            RemisePourcentage = 0m,
            DelaiEcheanceEnJours = 0,
            TarifParDefaut = 0m,
            ModePaiementParDefaut = new string('a', LongueursFacture.ModePaiement),
        };

        var resultat = _validateur.Validate(null, options);

        Assert.True(resultat.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Validate_DeviseDeLongueurIncorrecte_Echoue(string devise)
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation { Devise = devise });

        Assert.Contains("Devise", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_TauxTvaHorsBornes_Echoue(double taux)
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation { TauxTva = (decimal)taux });

        Assert.Contains("TauxTva", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_RemiseHorsBornes_Echoue(double remise)
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation { RemisePourcentage = (decimal)remise });

        Assert.Contains("RemisePourcentage", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_DelaiEcheanceNegatif_Echoue()
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation { DelaiEcheanceEnJours = -1 });

        Assert.Contains("DelaiEcheanceEnJours", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_TarifParDefautNegatif_Echoue()
    {
        var resultat = _validateur.Validate(null, new OptionsFacturation { TarifParDefaut = -1m });

        Assert.Contains("tarifs", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_TarifDeFiliereNegatif_Echoue()
    {
        var options = new OptionsFacturation();
        options.TarifsParFiliere["Elevage"] = -5m;

        var resultat = _validateur.Validate(null, options);

        Assert.Contains("tarifs", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ModePaiementTropLong_Echoue()
    {
        var options = new OptionsFacturation { ModePaiementParDefaut = new string('a', LongueursFacture.ModePaiement + 1) };

        var resultat = _validateur.Validate(null, options);

        Assert.Contains("ModePaiementParDefaut", Assert.Single(resultat.Failures!), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_PlusieursReglesViolees_CumuleLesErreurs()
    {
        var options = new OptionsFacturation
        {
            Devise = "EURO",
            TauxTva = 120m,
            RemisePourcentage = -1m,
            DelaiEcheanceEnJours = -3,
            TarifParDefaut = -1m,
            ModePaiementParDefaut = new string('a', LongueursFacture.ModePaiement + 1),
        };

        var resultat = _validateur.Validate(null, options);

        Assert.True(resultat.Failed);
        Assert.Equal(6, resultat.Failures!.Count());
    }
}
