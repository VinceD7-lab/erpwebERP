using erpWeb.Core.Communs;

namespace erpWeb.Core.Documents;

/// <summary>
/// Fichier rattaché de manière polymorphe à n'importe quelle entité (TypeEntite + IdEntite).
/// </summary>
public class Document : EntiteAuditable
{
    public string TypeEntite { get; set; } = string.Empty;

    public string IdEntite { get; set; } = string.Empty;

    public string NomFichier { get; set; } = string.Empty;

    public string TypeContenu { get; set; } = string.Empty;

    public long Taille { get; set; }

    public string CheminStockage { get; set; } = string.Empty;
}
