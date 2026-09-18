using erpWeb.Core.Communs;

namespace erpWeb.Core.Parametres;

/// <summary>
/// Paramètre clé/valeur modifiable sans redéploiement. La clé suit la notation
/// de configuration .NET (Section:Cle) et alimente les options typées IOptions&lt;T&gt;.
/// </summary>
public class Parametre : EntiteAuditable
{
    public string Cle { get; set; } = string.Empty;

    public string Valeur { get; set; } = string.Empty;

    public string? Description { get; set; }
}
