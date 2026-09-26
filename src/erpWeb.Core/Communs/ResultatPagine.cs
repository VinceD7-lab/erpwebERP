namespace erpWeb.Core.Communs;

/// <summary>Page de résultats d'une recherche paginée côté serveur.</summary>
public sealed record ResultatPagine<T>(IReadOnlyList<T> Elements, int NumeroPage, int TaillePage, int NombreTotal)
{
    /// <summary>
    /// Nombre total de pages, au minimum 1 pour qu'une grille vide reste affichable.
    /// La taille est ramenée à 1 au minimum : le type étant public, un appelant pourrait
    /// le construire avec une taille nulle, ce qui produirait un nombre de pages absurde.
    /// </summary>
    public int NombrePages => NombreTotal == 0 ? 1 : (int)Math.Ceiling(NombreTotal / (double)Math.Max(TaillePage, 1));
}
