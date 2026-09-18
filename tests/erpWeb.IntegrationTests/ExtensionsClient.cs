using System.Net;
using System.Text.RegularExpressions;

namespace erpWeb.IntegrationTests;

internal static partial class ExtensionsClient
{
    public static async Task<string> ObtenirJetonAntiforgeryAsync(this HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var correspondance = ExpressionJetonAntiforgery().Match(html);
        Assert.True(correspondance.Success, $"Jeton antiforgery introuvable sur {url}.");
        return WebUtility.HtmlDecode(correspondance.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> EnvoyerFormulaireAsync(this HttpClient client, string url, Dictionary<string, string> champs)
    {
        champs["__RequestVerificationToken"] = await client.ObtenirJetonAntiforgeryAsync(url);
        return await client.PostAsync(url, new FormUrlEncodedContent(champs));
    }

    public static async Task ConnecterAsync(this HttpClient client, string email, string motDePasse)
    {
        var reponse = await client.EnvoyerFormulaireAsync("/Compte/Connexion", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["MotDePasse"] = motDePasse,
        });

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex ExpressionJetonAntiforgery();
}
