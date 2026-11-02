using erpWeb.Core.DevisClients;

namespace erpWeb.UnitTests.Core;

public sealed class CalculateurDevisTests
{
    private readonly CalculateurDevis _calculateur = new();

    [Fact]
    public void Calculer_UneLigneSansRemise_CalculeMontantEtTotaux()
    {
        var devis = CreerDevis(Ligne(quantite: 3m, prix: 10m, taux: 20m));

        _calculateur.Calculer(devis);

        Assert.Equal(30m, devis.Lignes[0].MontantHorsTaxe);
        Assert.Equal(30m, devis.MontantTotalHorsTaxe);
        Assert.Equal(6m, devis.MontantTotalTaxe);
        Assert.Equal(36m, devis.MontantTotalToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_RemiseDeLigne_ReduitLeMontantDeLaLigne()
    {
        var devis = CreerDevis(Ligne(quantite: 2m, prix: 50m, taux: 20m, remise: 10m));

        _calculateur.Calculer(devis);

        Assert.Equal(90m, devis.Lignes[0].MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_RemiseGlobale_ReduitTotalHorsTaxeEtTaxe()
    {
        var devis = CreerDevis(Ligne(quantite: 1m, prix: 200m, taux: 20m));
        devis.TauxRemiseGlobale = 10m;

        _calculateur.Calculer(devis);

        Assert.Equal(180m, devis.MontantTotalHorsTaxe);
        Assert.Equal(36m, devis.MontantTotalTaxe);
        Assert.Equal(216m, devis.MontantTotalToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_TauxDeTaxeDifferentsParLigne_SommeLesTaxesParLigne()
    {
        var devis = CreerDevis(
            Ligne(quantite: 1m, prix: 100m, taux: 20m),
            Ligne(quantite: 1m, prix: 100m, taux: 5.5m));

        _calculateur.Calculer(devis);

        Assert.Equal(200m, devis.MontantTotalHorsTaxe);
        Assert.Equal(25.5m, devis.MontantTotalTaxe);
        Assert.Equal(225.5m, devis.MontantTotalToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_MontantAvecDecimalesLongues_ArrondiAuCentime()
    {
        var devis = CreerDevis(Ligne(quantite: 1.333m, prix: 10.01m, taux: 20m));

        _calculateur.Calculer(devis);

        Assert.Equal(13.34m, devis.Lignes[0].MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_SansLigne_TotauxNuls()
    {
        var devis = CreerDevis();

        _calculateur.Calculer(devis);

        Assert.Equal(0m, devis.MontantTotalHorsTaxe);
        Assert.Equal(0m, devis.MontantTotalToutesTaxesComprises);
    }

    private static Devis CreerDevis(params LigneDevis[] lignes) => new() { Lignes = [.. lignes] };

    private static LigneDevis Ligne(decimal quantite, decimal prix, decimal taux, decimal? remise = null)
        => new() { Quantite = quantite, PrixUnitaireHorsTaxe = prix, TauxTaxe = taux, TauxRemise = remise };
}
