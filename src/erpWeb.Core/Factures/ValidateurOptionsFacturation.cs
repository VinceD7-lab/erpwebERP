using Microsoft.Extensions.Options;

namespace erpWeb.Core.Factures;

/// <summary>Refuse au démarrage une configuration de facturation que la base rejetterait à l'écriture.</summary>
public sealed class ValidateurOptionsFacturation : IValidateOptions<OptionsFacturation>
{
    public ValidateOptionsResult Validate(string? nom, OptionsFacturation options)
    {
        var erreurs = new List<string>();

        if (options.Devise is not { Length: LongueursFacture.Devise })
        {
            erreurs.Add($"Facturation:Devise doit contenir exactement {LongueursFacture.Devise} caractères.");
        }

        if (options.TauxTva is < 0 or > 100)
        {
            erreurs.Add("Facturation:TauxTva doit être compris entre 0 et 100.");
        }

        if (options.RemisePourcentage is < 0 or > 100)
        {
            erreurs.Add("Facturation:RemisePourcentage doit être comprise entre 0 et 100.");
        }

        if (options.DelaiEcheanceEnJours < 0)
        {
            erreurs.Add("Facturation:DelaiEcheanceEnJours ne peut pas être négatif.");
        }

        if (options.TarifParDefaut < 0 || options.TarifsParFiliere.Values.Any(tarif => tarif < 0))
        {
            erreurs.Add("Les tarifs de facturation ne peuvent pas être négatifs.");
        }

        if (options.ModePaiementParDefaut.Length > LongueursFacture.ModePaiement)
        {
            erreurs.Add($"Facturation:ModePaiementParDefaut ne peut pas dépasser {LongueursFacture.ModePaiement} caractères.");
        }

        return erreurs.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(erreurs);
    }
}
