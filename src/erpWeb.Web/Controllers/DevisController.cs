using erpWeb.Core.Autorisation;
using erpWeb.Core.DevisClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Devis.Lire)]
public sealed class DevisController : Controller
{
    private readonly IRechercheDevis _rechercheDevis;

    public DevisController(IRechercheDevis rechercheDevis)
    {
        _rechercheDevis = rechercheDevis;
    }

    /// <summary>La grille charge ses données par <see cref="Resultats"/>.</summary>
    public IActionResult Index() => View();

    /// <summary>Page de résultats consommée par la grille (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Resultats([FromQuery] CriteresDevis criteres, CancellationToken jetonAnnulation)
    {
        // Les bornes de pagination sont ramenées par le service ; ModelState ne refuse que
        // ce qui n'a pas pu être lié (colonne de tri inconnue).
        if (!ModelState.IsValid)
        {
            return BadRequest(new { erreur = "Critères de recherche invalides." });
        }

        return Json(await _rechercheDevis.RechercherAsync(criteres, jetonAnnulation));
    }
}
