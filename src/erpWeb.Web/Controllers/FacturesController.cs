using erpWeb.Core.Autorisation;
using erpWeb.Core.Clients;
using erpWeb.Core.Factures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Factures.Lire)]
public sealed class FacturesController : Controller
{
    private readonly IGenerationFactures _generationFactures;
    private readonly IRechercheFactures _rechercheFactures;
    private readonly IServiceImpressionFacture _impressionFacture;
    private readonly IServiceClients _serviceClients;

    public FacturesController(
        IGenerationFactures generationFactures,
        IRechercheFactures rechercheFactures,
        IServiceImpressionFacture impressionFacture,
        IServiceClients serviceClients)
    {
        _generationFactures = generationFactures;
        _rechercheFactures = rechercheFactures;
        _impressionFacture = impressionFacture;
        _serviceClients = serviceClients;
    }

    /// <summary>Génère les factures manquantes, puis affiche la page ; la grille charge ses données par <see cref="Resultats"/>.</summary>
    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
    {
        await _generationFactures.GenererManquantesAsync(jetonAnnulation);
        return View(await _serviceClients.ListerAsync(jetonAnnulation));
    }

    /// <summary>Page de résultats consommée par la grille (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Resultats([FromQuery] CriteresFactures criteres, CancellationToken jetonAnnulation)
    {
        // Les bornes de pagination sont ramenées par le service ; ModelState ne refuse que
        // ce qui n'a pas pu être lié (colonne de tri inconnue, date non analysable).
        if (!ModelState.IsValid)
        {
            return BadRequest(new { erreur = "Critères de recherche invalides." });
        }

        return Json(await _rechercheFactures.RechercherAsync(criteres, jetonAnnulation));
    }

    /// <summary>Page de facture mise en forme pour l'impression.</summary>
    [HttpGet]
    public async Task<IActionResult> Imprimer(int id, CancellationToken jetonAnnulation)
    {
        var impression = await _impressionFacture.ObtenirAsync(id, jetonAnnulation);
        return impression is null ? NotFound() : View(impression);
    }
}
