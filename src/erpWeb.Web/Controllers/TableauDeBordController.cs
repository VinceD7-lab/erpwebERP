using erpWeb.Core.TableauDeBord;
using erpWeb.Web.ViewComponents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

public sealed class TableauDeBordController : Controller
{
    private readonly IEnumerable<IWidgetTableauDeBord> _widgets;
    private readonly IAuthorizationService _serviceAutorisation;

    public TableauDeBordController(IEnumerable<IWidgetTableauDeBord> widgets, IAuthorizationService serviceAutorisation)
    {
        _widgets = widgets;
        _serviceAutorisation = serviceAutorisation;
    }

    public async Task<IActionResult> Index()
        => View(await ListerWidgetsAutorisesAsync());

    /// <summary>Rafraîchissement à la demande d'un widget (appel fetch).</summary>
    [HttpGet]
    public async Task<IActionResult> Widget(string id)
    {
        var widget = (await ListerWidgetsAutorisesAsync()).FirstOrDefault(w => w.Nom == id);
        return widget is null ? NotFound() : ViewComponent(typeof(WidgetViewComponent), new { widget });
    }

    private async Task<IReadOnlyList<IWidgetTableauDeBord>> ListerWidgetsAutorisesAsync()
    {
        var autorises = new List<IWidgetTableauDeBord>();
        foreach (var widget in _widgets.OrderBy(w => w.Ordre))
        {
            if (widget.PermissionRequise is null
                || (await _serviceAutorisation.AuthorizeAsync(User, widget.PermissionRequise)).Succeeded)
            {
                autorises.Add(widget);
            }
        }

        return autorises;
    }
}
