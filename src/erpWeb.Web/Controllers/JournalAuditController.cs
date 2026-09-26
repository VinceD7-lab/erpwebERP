using System.Globalization;
using erpWeb.Core.Audit;
using erpWeb.Core.Autorisation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.JournalAudit.Lire)]
public sealed class JournalAuditController : Controller
{
    private const int NombreEntreesExportees = 10_000;

    private readonly ILectureJournalAudit _lectureJournalAudit;
    private readonly IRechercheJournalAudit _rechercheJournalAudit;
    private readonly IExportJournalAudit _exportJournalAudit;
    private readonly TimeProvider _horloge;

    public JournalAuditController(
        ILectureJournalAudit lectureJournalAudit,
        IRechercheJournalAudit rechercheJournalAudit,
        IExportJournalAudit exportJournalAudit,
        TimeProvider horloge)
    {
        _lectureJournalAudit = lectureJournalAudit;
        _rechercheJournalAudit = rechercheJournalAudit;
        _exportJournalAudit = exportJournalAudit;
        _horloge = horloge;
    }

    /// <summary>La grille charge ses données par l'action <see cref="Entrees"/>.</summary>
    public IActionResult Index() => View();

    /// <summary>Page de résultats consommée par la grille (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Entrees([FromQuery] CriteresJournalAudit criteres, CancellationToken jetonAnnulation)
    {
        // Les bornes de pagination sont ramenées par le service ; ModelState ne refuse que
        // ce qui n'a pas pu être lié (colonne de tri inconnue, date non analysable).
        if (!ModelState.IsValid)
        {
            return BadRequest(new { erreur = "Critères de recherche invalides." });
        }

        return Json(await _rechercheJournalAudit.RechercherAsync(criteres, jetonAnnulation));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.JournalAudit.Exporter)]
    public async Task<IActionResult> Exporter(CancellationToken jetonAnnulation)
    {
        var entrees = await _lectureJournalAudit.ListerRecentesAsync(NombreEntreesExportees, jetonAnnulation);
        var horodatage = _horloge.GetUtcNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return File(_exportJournalAudit.Exporter(entrees), _exportJournalAudit.TypeContenu, $"journal-audit-{horodatage}{_exportJournalAudit.ExtensionFichier}");
    }
}
