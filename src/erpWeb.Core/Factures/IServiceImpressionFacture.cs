namespace erpWeb.Core.Factures;

public interface IServiceImpressionFacture
{
    /// <summary>Renvoie les données de la page imprimable, ou <c>null</c> si la facture n'existe pas.</summary>
    Task<ImpressionFactureDto?> ObtenirAsync(int idFacture, CancellationToken jetonAnnulation = default);
}
