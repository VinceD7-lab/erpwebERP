namespace erpWeb.Core.Communs;

/// <summary>
/// Classe de base des entités tracées : les champs sont alimentés automatiquement
/// par l'intercepteur d'audit lors de l'enregistrement.
/// </summary>
public abstract class EntiteAuditable
{
    public int Id { get; set; }

    public DateTime DateCreation { get; set; }

    public string? CreePar { get; set; }

    public DateTime? DateModification { get; set; }

    public string? ModifiePar { get; set; }
}
