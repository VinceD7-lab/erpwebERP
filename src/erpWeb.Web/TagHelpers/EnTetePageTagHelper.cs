using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace erpWeb.Web.TagHelpers;

/// <summary>
/// En-tête de page commun : &lt;en-tete-page titre="Utilisateurs" icone="bi-people"&gt;boutons d'action&lt;/en-tete-page&gt;.
/// </summary>
[HtmlTargetElement("en-tete-page")]
public sealed class EnTetePageTagHelper : TagHelper
{
    public string Titre { get; set; } = string.Empty;

    public string? Icone { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var actions = (await output.GetChildContentAsync()).GetContent();
        var encodeur = HtmlEncoder.Default;
        var icone = string.IsNullOrEmpty(Icone) ? string.Empty : $"<i class=\"bi {encodeur.Encode(Icone)} me-2\" aria-hidden=\"true\"></i>";

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4");
        output.Content.SetHtmlContent(
            $"<h1 class=\"h3 mb-0\">{icone}{encodeur.Encode(Titre)}</h1><div class=\"d-flex flex-wrap gap-2\">{actions}</div>");
    }
}
