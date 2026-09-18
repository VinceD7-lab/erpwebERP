using erpWeb.Core.Audit;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWeb.Core.Email;
using erpWeb.Core.Parametres;
using erpWeb.Core.Utilisateurs;
using erpWeb.Infrastructure.Configuration;
using erpWeb.Infrastructure.Donnees;
using erpWeb.Infrastructure.Email;
using erpWeb.Infrastructure.Exports;
using erpWeb.Infrastructure.Identite;
using erpWeb.Infrastructure.Stockage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.Infrastructure;

/// <summary>Composition root de la couche Infrastructure, appelée uniquement depuis Program.cs.</summary>
public static class DependencyInjection
{
    public const string NomChaineConnexion = "ParDefaut";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string repertoireContenu)
    {
        var chaineConnexion = ObtenirChaineConnexion(configuration);

        services.Configure<OptionsSmtp>(configuration.GetSection(OptionsSmtp.Section));
        services.Configure<OptionsAdministrateur>(configuration.GetSection(OptionsAdministrateur.Section));
        services.Configure<OptionsStockage>(configuration.GetSection(OptionsStockage.Section));
        services.PostConfigure<OptionsStockage>(options => options.Dossier = Path.Combine(repertoireContenu, options.Dossier));

        services.AddScoped<IntercepteurAudit>();
        services.AddDbContext<AppDbContext>((fournisseur, options) => options
            .UseSqlServer(chaineConnexion)
            .AddInterceptors(fournisseur.GetRequiredService<IntercepteurAudit>()));
        services.AddScoped<IAppDbContext>(fournisseur => fournisseur.GetRequiredService<AppDbContext>());

        services.AddScoped<IServiceEmail, ServiceEmailMailKit>();
        services.AddScoped<IStockageFichiers, StockageFichiersLocal>();
        services.AddScoped<IExportJournalAudit, ExportJournalAuditExcel>();
        services.AddSingleton<IRechargementConfiguration, RechargementConfiguration>();
        services.AddScoped<InitialisateurDonnees>();

        return services;
    }

    public static IdentityBuilder AddStockageIdentite(this IdentityBuilder identite)
        => identite
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<DescripteurErreursIdentiteFrancais>();

    /// <summary>Ajoute la table Parametres comme dernière source : elle surcharge les fichiers de configuration.</summary>
    public static ConfigurationManager AddParametresBaseDeDonnees(this ConfigurationManager configuration)
    {
        ((IConfigurationBuilder)configuration).Add(new SourceConfigurationParametres(ObtenirChaineConnexion(configuration)));
        return configuration;
    }

    public static async Task InitialiserDonneesAsync(this IServiceProvider services, bool appliquerMigrations, CancellationToken jetonAnnulation = default)
    {
        await using var portee = services.CreateAsyncScope();
        var initialisateur = portee.ServiceProvider.GetRequiredService<InitialisateurDonnees>();
        await initialisateur.InitialiserAsync(appliquerMigrations, jetonAnnulation);
    }

    private static string ObtenirChaineConnexion(IConfiguration configuration)
        => configuration.GetConnectionString(NomChaineConnexion)
            ?? throw new InvalidOperationException($"Chaîne de connexion « {NomChaineConnexion} » absente de la configuration.");
}
