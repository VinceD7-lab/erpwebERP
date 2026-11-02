using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

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

    /// <summary>Client connecté avec un compte créé par l'administrateur sans aucun rôle, donc sans permission.</summary>
    public static async Task<HttpClient> CreerClientSansPermissionAsync(this FabriqueApplication fabrique)
    {
        var email = $"sans-role-{Guid.NewGuid():N}@erpweb.local";
        const string motDePasse = "SansRole#2026";

        var administrateur = fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await administrateur.ConnecterAsync(FabriqueApplication.EmailAdministrateur, FabriqueApplication.MotDePasseAdministrateur);
        var creation = await administrateur.EnvoyerFormulaireAsync("/Utilisateurs/Creer", new Dictionary<string, string>
        {
            ["NomComplet"] = "Compte Sans Role",
            ["Email"] = email,
            ["MotDePasse"] = motDePasse,
        });
        Assert.Equal(HttpStatusCode.Redirect, creation.StatusCode);

        var client = fabrique.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.ConnecterAsync(email, motDePasse);
        return client;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex ExpressionJetonAntiforgery();
}
