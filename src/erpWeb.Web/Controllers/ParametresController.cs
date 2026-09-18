using erpWeb.Core.Autorisation;
using erpWeb.Core.Parametres;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Parametres.Lire)]
public sealed class ParametresController : Controller
{
    public const string CleEstNouveau = "EstNouveau";

    private readonly IServiceParametres _serviceParametres;

    public ParametresController(IServiceParametres serviceParametres)
    {
        _serviceParametres = serviceParametres;
    }

    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
        => View(await _serviceParametres.ListerAsync(jetonAnnulation));

    [HttpGet]
    [Authorize(Policy = Permissions.Parametres.Modifier)]
    public async Task<IActionResult> Modifier(string? cle, CancellationToken jetonAnnulation)
    {
        if (string.IsNullOrEmpty(cle))
        {
            ViewData[CleEstNouveau] = true;
            return View(new ModificationParametreDto());
        }

        var parametre = await _serviceParametres.ObtenirAsync(cle, jetonAnnulation);
        if (parametre is null)
        {
            return NotFound();
        }

        return View(new ModificationParametreDto { Cle = parametre.Cle, Valeur = parametre.Valeur, Description = parametre.Description });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Parametres.Modifier)]
    public async Task<IActionResult> Modifier(ModificationParametreDto modification, bool estNouveau, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceParametres.DefinirAsync(modification, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            ViewData[CleEstNouveau] = estNouveau;
            return View(modification);
        }

        TempData[CleMessages.Succes] = $"Le paramètre {modification.Cle} a été enregistré.";
        return RedirectToAction(nameof(Index));
    }
}
