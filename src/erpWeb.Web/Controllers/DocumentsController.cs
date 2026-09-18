using erpWeb.Core.Autorisation;
using erpWeb.Core.Documents;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Documents.Lire)]
public sealed class DocumentsController : Controller
{
    private readonly IServiceDocuments _serviceDocuments;

    public DocumentsController(IServiceDocuments serviceDocuments)
    {
        _serviceDocuments = serviceDocuments;
    }

    public async Task<IActionResult> Index(string? typeEntite, string? idEntite, CancellationToken jetonAnnulation)
    {
        ViewData[nameof(typeEntite)] = typeEntite;
        ViewData[nameof(idEntite)] = idEntite;
        return View(await _serviceDocuments.ListerAsync(typeEntite, idEntite, jetonAnnulation));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Documents.Deposer)]
    public IActionResult Deposer() => View(new DepotDocumentViewModel());

    [HttpPost]
    [Authorize(Policy = Permissions.Documents.Deposer)]
    public async Task<IActionResult> Deposer(DepotDocumentViewModel depot, CancellationToken jetonAnnulation)
    {
        if (!ModelState.IsValid || depot.Fichier is null)
        {
            return View(depot);
        }

        await using var contenu = depot.Fichier.OpenReadStream();
        var resultat = await _serviceDocuments.DeposerAsync(
            new DepotDocumentDto
            {
                TypeEntite = depot.TypeEntite,
                IdEntite = depot.IdEntite,
                NomFichier = Path.GetFileName(depot.Fichier.FileName),
                TypeContenu = string.IsNullOrEmpty(depot.Fichier.ContentType) ? "application/octet-stream" : depot.Fichier.ContentType,
                Taille = depot.Fichier.Length,
                Contenu = contenu,
            },
            jetonAnnulation);

        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            return View(depot);
        }

        TempData[CleMessages.Succes] = $"Le document {depot.Fichier.FileName} a été déposé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Telecharger(int id, CancellationToken jetonAnnulation)
    {
        var fichier = await _serviceDocuments.OuvrirAsync(id, jetonAnnulation);
        return fichier is null
            ? NotFound()
            : File(fichier.Contenu, fichier.Document.TypeContenu, fichier.Document.NomFichier);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Documents.Supprimer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceDocuments.SupprimerAsync(id, jetonAnnulation);
        if (resultat.Reussi)
        {
            TempData[CleMessages.Succes] = "Le document a été supprimé.";
        }
        else
        {
            TempData[CleMessages.Erreur] = string.Join(" ", resultat.Erreurs);
        }

        return RedirectToAction(nameof(Index));
    }
}
