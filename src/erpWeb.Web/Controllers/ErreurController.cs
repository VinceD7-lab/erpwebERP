using System.Diagnostics;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[AllowAnonymous]
public sealed class ErreurController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
        => View(new ErreurViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
