using System.Globalization;
using System.Text.Json.Serialization;
using erpWeb.Core;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Factures;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure;
using erpWeb.Web.Autorisation;
using erpWeb.Web.Securite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var repertoireContenu = builder.Environment.ContentRootPath;

// Paramètres en base : dernière source, ils surchargent les fichiers de configuration.
builder.Configuration.AddParametresBaseDeDonnees();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourantHttp>();

builder.Services.Configure<OptionsFacturation>(builder.Configuration.GetSection(OptionsFacturation.Section));
builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration, repertoireContenu, environnementDeveloppement: builder.Environment.IsDevelopment());

builder.Services
    .AddIdentity<Utilisateur, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddStockageIdentite()
    .AddSignInManager<GestionnaireConnexion>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Compte/Connexion";
    options.LogoutPath = "/Compte/Deconnexion";
    options.AccessDeniedPath = "/Compte/AccesRefuse";
    options.Cookie.Name = "erpWeb.Authentification";
    options.SlidingExpiration = true;

    // Les appels JSON (îlots Vue) doivent recevoir un statut exploitable, pas une page de connexion HTML.
    options.Events.OnRedirectToLogin = contexte => RepondreSansRedirection(contexte, StatusCodes.Status401Unauthorized);
    options.Events.OnRedirectToAccessDenied = contexte => RepondreSansRedirection(contexte, StatusCodes.Status403Forbidden);
});

// Un compte désactivé (jeton de sécurité renouvelé) est déconnecté en moins d'une minute.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    foreach (var permission in Permissions.Toutes)
    {
        options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new ExigencePermission(permission)));
    }
});
builder.Services.AddSingleton<IAuthorizationHandler, GestionnairePermission>();

builder.Services
    .AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    // Les énumérations sont exposées par leur nom : un client JavaScript ne dépend pas
    // de l'ordre de déclaration côté C#.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

await app.Services.InitialiserDonneesAsync(
    appliquerMigrations: app.Configuration.GetValue<bool>("BaseDeDonnees:AppliquerMigrationsAuDemarrage"));

if (app.Environment.IsDevelopment())
{
    await app.Services.SemerClientsDemonstrationAsync(quantite: 25);
    await app.Services.SemerEchantillonsDemonstrationAsync(nombreTournees: 15);
}

var cultureFrancaise = new CultureInfo("fr-FR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureFrancaise),
    SupportedCultures = [cultureFrancaise],
    SupportedUICultures = [cultureFrancaise],
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erreur");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "defaut",
    pattern: "{controller=TableauDeBord}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

/// <summary>
/// Répond par un statut plutôt que par une redirection quand l'appelant attend du JSON.
/// Une navigation ordinaire conserve la redirection vers la page de connexion.
/// </summary>
static Task RepondreSansRedirection(RedirectContext<CookieAuthenticationOptions> contexte, int statut)
{
    if (AttendDuJson(contexte.Request))
    {
        contexte.Response.StatusCode = statut;
        return Task.CompletedTask;
    }

    contexte.Response.Redirect(contexte.RedirectUri);
    return Task.CompletedTask;
}

static bool AttendDuJson(HttpRequest requete)
    => requete.Headers.XRequestedWith == "fetch"
        || requete.Headers.Accept.Any(valeur => valeur is not null && valeur.Contains("application/json", StringComparison.OrdinalIgnoreCase));

/// <summary>Point d'entrée exposé aux tests d'intégration (WebApplicationFactory).</summary>
public partial class Program;
