using erpWeb.Core.TableauDeBord;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.ViewComponents;

/// <summary>
/// Rend un widget avec la vue associée à son type de données
/// (Views/Shared/Components/Widget/{TypeDonnees}.cshtml).
/// </summary>
public sealed class WidgetViewComponent : ViewComponent
{
    public const string CleTitreWidget = "TitreWidget";

    public async Task<IViewComponentResult> InvokeAsync(IWidgetTableauDeBord widget)
    {
        var donnees = await widget.ObtenirDonneesAsync(HttpContext.RequestAborted);
        ViewData[CleTitreWidget] = widget.Titre;
        return View(donnees.GetType().Name, donnees);
    }
}
