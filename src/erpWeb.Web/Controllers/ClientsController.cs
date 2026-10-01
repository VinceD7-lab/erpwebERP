using erpWeb.Core.Autorisation;
using erpWeb.Core.Clients;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Clients.Lire)]
public sealed class ClientsController : Controller
{
    private readonly IServiceClients _serviceClients;

    public ClientsController(IServiceClients serviceClients)
    {
        _serviceClients = serviceClients;
    }

    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
        => View(await _serviceClients.ListerAsync(jetonAnnulation));

    [HttpGet]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public IActionResult Creer() => View(new CreationClientDto());

    [HttpPost]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> Creer(CreationClientDto creation, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceClients.CreerAsync(creation, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            return View(creation);
        }

        TempData[CleMessages.Succes] = $"Le client {creation.RaisonSociale} a été créé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> Modifier(int id, CancellationToken jetonAnnulation)
    {
        var modification = await _serviceClients.ObtenirPourModificationAsync(id, jetonAnnulation);
        return modification is null ? NotFound() : View(modification);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> Modifier(ModificationClientDto modification, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceClients.ModifierAsync(modification, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            return View(modification);
        }

        TempData[CleMessages.Succes] = "Les modifications ont été enregistrées.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> LigneLecture(int id, CancellationToken jetonAnnulation)
    {
        var client = await _serviceClients.ObtenirAsync(id, jetonAnnulation);
        return client is null ? NotFound() : PartialView("_LigneClient", client);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> LigneEdition(int id, CancellationToken jetonAnnulation)
    {
        var modification = await _serviceClients.ObtenirPourModificationAsync(id, jetonAnnulation);
        return modification is null ? NotFound() : PartialView("_LigneClientEdition", modification);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> ModifierLigne(ModificationClientDto modification, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceClients.ModifierAsync(modification, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView("_LigneClientEdition", modification);
        }

        var client = await _serviceClients.ObtenirAsync(modification.Id, jetonAnnulation);
        return client is null ? NotFound() : PartialView("_LigneClient", client);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Clients.Gerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceClients.SupprimerAsync(id, jetonAnnulation);
        if (resultat.Reussi)
        {
            TempData[CleMessages.Succes] = "Le client a été supprimé.";
        }
        else
        {
            TempData[CleMessages.Erreur] = string.Join(" ", resultat.Erreurs);
        }

        return RedirectToAction(nameof(Index));
    }
}
