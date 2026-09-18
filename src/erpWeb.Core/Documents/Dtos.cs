namespace erpWeb.Core.Documents;

public sealed class DocumentDto
{
    public int Id { get; init; }

    public string TypeEntite { get; init; } = string.Empty;

    public string IdEntite { get; init; } = string.Empty;

    public string NomFichier { get; init; } = string.Empty;

    public string TypeContenu { get; init; } = string.Empty;

    public long Taille { get; init; }

    public DateTime DateCreation { get; init; }

    public string? CreePar { get; init; }
}

public sealed class DepotDocumentDto
{
    public string TypeEntite { get; init; } = string.Empty;

    public string IdEntite { get; init; } = string.Empty;

    public string NomFichier { get; init; } = string.Empty;

    public string TypeContenu { get; init; } = string.Empty;

    public long Taille { get; init; }

    public required Stream Contenu { get; init; }
}

public sealed record FichierDocument(DocumentDto Document, Stream Contenu);
