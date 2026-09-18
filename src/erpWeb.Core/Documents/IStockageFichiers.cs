namespace erpWeb.Core.Documents;

public interface IStockageFichiers
{
    /// <summary>Enregistre le contenu et retourne le chemin de stockage à conserver en base.</summary>
    Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken jetonAnnulation = default);

    Task<Stream> OuvrirAsync(string cheminStockage, CancellationToken jetonAnnulation = default);

    Task SupprimerAsync(string cheminStockage, CancellationToken jetonAnnulation = default);
}
