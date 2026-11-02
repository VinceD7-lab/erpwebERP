namespace erpWeb.Core.DevisClients;

public interface ICalculateurDevis
{
    /// <summary>Renseigne le montant de chaque ligne puis les totaux du devis.</summary>
    void Calculer(Devis devis);
}
