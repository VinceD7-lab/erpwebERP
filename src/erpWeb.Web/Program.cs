using System.Globalization;
using erpWeb.Core;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure;
using erpWeb.Web.Autorisation;
using erpWeb.Web.Securite;
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

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration, repertoireContenu);

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

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

await app.Services.InitialiserDonneesAsync(
    appliquerMigrations: app.Configuration.GetValue<bool>("BaseDeDonnees:AppliquerMigrationsAuDemarrage"));

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

/// <summary>Point d'entrée exposé aux tests d'intégration (WebApplicationFactory).</summary>
public partial class Program;
