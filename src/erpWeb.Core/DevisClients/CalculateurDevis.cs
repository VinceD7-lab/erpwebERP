namespace erpWeb.Core.DevisClients;

public sealed class CalculateurDevis : ICalculateurDevis
{
    private const decimal Cent = 100m;

    public void Calculer(Devis devis)
    {
        foreach (var ligne in devis.Lignes)
        {
            ligne.MontantHorsTaxe = Arrondir(ligne.Quantite * ligne.PrixUnitaireHorsTaxe * (1 - (ligne.TauxRemise ?? 0m) / Cent));
        }

        var coefficientRemiseGlobale = 1 - (devis.TauxRemiseGlobale ?? 0m) / Cent;
        var totalHorsTaxe = devis.Lignes.Sum(ligne => ligne.MontantHorsTaxe) * coefficientRemiseGlobale;
        var totalTaxe = devis.Lignes.Sum(ligne => ligne.MontantHorsTaxe * ligne.TauxTaxe / Cent) * coefficientRemiseGlobale;

        devis.MontantTotalHorsTaxe = Arrondir(totalHorsTaxe);
        devis.MontantTotalTaxe = Arrondir(totalTaxe);
        devis.MontantTotalToutesTaxesComprises = devis.MontantTotalHorsTaxe + devis.MontantTotalTaxe;
    }

    private static decimal Arrondir(decimal montant) => Math.Round(montant, 2, MidpointRounding.AwayFromZero);
}
