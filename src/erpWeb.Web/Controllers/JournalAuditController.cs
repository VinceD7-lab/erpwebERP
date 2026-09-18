using System.Globalization;
using erpWeb.Core.Audit;
using erpWeb.Core.Autorisation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.JournalAudit.Lire)]
public sealed class JournalAuditController : Controller
{
    private const int NombreEntreesAffichees = 500;
    private const int NombreEntreesExportees = 10_000;

    private readonly ILectureJournalAudit _lectureJournalAudit;
    private readonly IExportJournalAudit _exportJournalAudit;
    private readonly TimeProvider _horloge;

    public JournalAuditController(ILectureJournalAudit lectureJournalAudit, IExportJournalAudit exportJournalAudit, TimeProvider horloge)
    {
        _lectureJournalAudit = lectureJournalAudit;
        _exportJournalAudit = exportJournalAudit;
        _horloge = horloge;
    }

    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
        => View(await _lectureJournalAudit.ListerRecentesAsync(NombreEntreesAffichees, jetonAnnulation));

    [HttpGet]
    [Authorize(Policy = Permissions.JournalAudit.Exporter)]
    public async Task<IActionResult> Exporter(CancellationToken jetonAnnulation)
    {
        var entrees = await _lectureJournalAudit.ListerRecentesAsync(NombreEntreesExportees, jetonAnnulation);
        var horodatage = _horloge.GetUtcNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return File(_exportJournalAudit.Exporter(entrees), _exportJournalAudit.TypeContenu, $"journal-audit-{horodatage}{_exportJournalAudit.ExtensionFichier}");
    }
}
