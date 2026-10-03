namespace erpWeb.Core.Factures;

public interface IGenerationFactures
{
    /// <summary>
    /// Crée la facture de chaque échantillon rattaché à un client qui n'en a pas encore, et passe en retard
    /// les factures émises dont l'échéance est dépassée. Idempotent ; renvoie le nombre de factures créées.
    /// </summary>
    Task<int> GenererManquantesAsync(CancellationToken jetonAnnulation = default);
}
