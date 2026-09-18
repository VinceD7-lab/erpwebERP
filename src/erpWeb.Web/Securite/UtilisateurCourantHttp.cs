using System.Security.Claims;
using erpWeb.Core.Communs;

namespace erpWeb.Web.Securite;

public sealed class UtilisateurCourantHttp : IUtilisateurCourant
{
    private readonly IHttpContextAccessor _accesseurContexteHttp;

    public UtilisateurCourantHttp(IHttpContextAccessor accesseurContexteHttp)
    {
        _accesseurContexteHttp = accesseurContexteHttp;
    }

    public string? Identifiant => _accesseurContexteHttp.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? NomUtilisateur => _accesseurContexteHttp.HttpContext?.User.Identity?.Name;
}
