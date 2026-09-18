namespace erpWeb.Core.Parametres;

public sealed class ParametreDto
{
    public int Id { get; init; }

    public string Cle { get; init; } = string.Empty;

    public string Valeur { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateTime? DateModification { get; init; }

    public string? ModifiePar { get; init; }
}

public sealed class ModificationParametreDto
{
    public string Cle { get; set; } = string.Empty;

    public string Valeur { get; set; } = string.Empty;

    public string? Description { get; set; }
}
