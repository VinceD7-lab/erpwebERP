using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace erpWeb.Web.TagHelpers;

/// <summary>
/// Masque l'élément si l'utilisateur ne possède pas la permission :
/// &lt;a asp-permission="Utilisateurs.Gerer"&gt;. Une valeur vide laisse l'élément visible.
/// </summary>
[HtmlTargetElement(Attributes = NomAttribut)]
public sealed class PermissionTagHelper : TagHelper
{
    private const string NomAttribut = "asp-permission";

    private readonly IAuthorizationService _serviceAutorisation;

    public PermissionTagHelper(IAuthorizationService serviceAutorisation)
    {
        _serviceAutorisation = serviceAutorisation;
    }

    [HtmlAttributeName(NomAttribut)]
    public string? Permission { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(Permission))
        {
            return;
        }

        var resultat = await _serviceAutorisation.AuthorizeAsync(ViewContext.HttpContext.User, Permission);
        if (!resultat.Succeeded)
        {
            output.SuppressOutput();
        }
    }
}
