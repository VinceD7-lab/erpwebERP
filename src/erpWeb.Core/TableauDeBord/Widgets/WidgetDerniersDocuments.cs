using System.Globalization;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Documents;

namespace erpWeb.Core.TableauDeBord.Widgets;

public sealed class WidgetDerniersDocuments : IWidgetTableauDeBord
{
    private const int NombreDocuments = 5;

    private readonly IServiceDocuments _serviceDocuments;

    public WidgetDerniersDocuments(IServiceDocuments serviceDocuments)
    {
        _serviceDocuments = serviceDocuments;
    }

    public string Nom => "derniers-documents";

    public string Titre => "Derniers documents";

    public string Icone => "bi-file-earmark-text";

    public int Ordre => 30;

    public string? PermissionRequise => Permissions.Documents.Lire;

    public async Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default)
    {
        var documents = await _serviceDocuments.ListerRecentsAsync(NombreDocuments, jetonAnnulation);

        var elements = documents
            .Select(document => new ElementListe(
                document.NomFichier,
                $"{document.TypeEntite} #{document.IdEntite} · {document.DateCreation.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}",
                $"/Documents/Telecharger/{document.Id}"))
            .ToList();

        return new DonneesListe(elements, "Aucun document déposé.");
    }
}
