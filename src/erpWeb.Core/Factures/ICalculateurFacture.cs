using erpWeb.Core.Echantillons;

namespace erpWeb.Core.Factures;

public interface ICalculateurFacture
{
    /// <summary>Calcule la facture d'un échantillon dont le client et le résultat sont chargés (numéro non attribué).</summary>
    Facture Calculer(Echantillon echantillon);
}
