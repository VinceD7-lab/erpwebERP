using erpWeb.Core.Autorisation;
using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Echantillons.Lire)]
public sealed class EchantillonsController : Controller
{
    private readonly IRechercheResultatsEchantillons _rechercheResultats;
    private readonly IServiceClients _serviceClients;

    public EchantillonsController(IRechercheResultatsEchantillons rechercheResultats, IServiceClients serviceClients)
    {
        _rechercheResultats = rechercheResultats;
        _serviceClients = serviceClients;
    }

    /// <summary>La grille charge ses données par l'action <see cref="Resultats"/> ; les clients alimentent le filtre.</summary>
    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
        => View(await _serviceClients.ListerAsync(jetonAnnulation));

    /// <summary>Page de résultats consommée par la grille (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Resultats([FromQuery] CriteresResultatsEchantillons criteres, CancellationToken jetonAnnulation)
    {
        // Les bornes de pagination sont ramenées par le service ; ModelState ne refuse que
        // ce qui n'a pas pu être lié (colonne de tri inconnue, date non analysable).
        if (!ModelState.IsValid)
        {
            return BadRequest(new { erreur = "Critères de recherche invalides." });
        }

        return Json(await _rechercheResultats.RechercherAsync(criteres, jetonAnnulation));
    }
}
