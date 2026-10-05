using erpWeb.Core.Autorisation;
using erpWeb.Core.PlanningAnalyses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.PlanningAnalyses.Lire)]
public sealed class PlanningAnalysesController : Controller
{
    private readonly IServicePlanningAnalyses _servicePlanning;

    public PlanningAnalysesController(IServicePlanningAnalyses servicePlanning)
    {
        _servicePlanning = servicePlanning;
    }

    /// <summary>La grille charge le mois par l'action <see cref="Mois"/> ; sans paramètre, le mois courant s'affiche.</summary>
    public IActionResult Index(int? annee, int? mois)
        => View(new PeriodePlanning(annee, mois));

    /// <summary>Planning d'un mois (JSON), consommé par la grille.</summary>
    [HttpGet]
    public async Task<IActionResult> Mois(int? annee, int? mois, CancellationToken jetonAnnulation)
        => Json(await _servicePlanning.ObtenirMoisAsync(annee, mois, jetonAnnulation));

    /// <summary>Enregistre le planning complet d'un mois (corps JSON, jeton antiforgery en en-tête).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PlanningAnalyses.Gerer)]
    public async Task<IActionResult> Sauvegarder([FromBody] SauvegardePlanningDto planning, CancellationToken jetonAnnulation)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { erreurs = new[] { "Le planning transmis est invalide." } });
        }

        var resultat = await _servicePlanning.SauvegarderMoisAsync(planning, jetonAnnulation);
        return resultat.Reussi ? Ok(new { reussi = true }) : BadRequest(new { erreurs = resultat.Erreurs });
    }
}

/// <summary>Mois demandé dans l'adresse de la page (facultatif).</summary>
public sealed record PeriodePlanning(int? Annee, int? Mois);
