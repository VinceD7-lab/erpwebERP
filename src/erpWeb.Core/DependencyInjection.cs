using erpWeb.Core.Audit;
using erpWeb.Core.Documents;
using erpWeb.Core.Parametres;
using erpWeb.Core.TableauDeBord;
using erpWeb.Core.Utilisateurs;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var assemblage = typeof(DependencyInjection).Assembly;

        services.AddScoped<IServiceUtilisateurs, ServiceUtilisateurs>();
        services.AddScoped<ILectureJournalAudit, ServiceJournalAudit>();
        services.AddScoped<IServiceDocuments, ServiceDocuments>();
        services.AddScoped<IServiceParametres, ServiceParametres>();

        services.AddValidatorsFromAssembly(assemblage, ServiceLifetime.Scoped, includeInternalTypes: false);

        // Instance dédiée (pas de TypeAdapterConfig.GlobalSettings statique) : une configuration IRegister par module.
        var configurationMapping = new TypeAdapterConfig();
        configurationMapping.Scan(assemblage);
        services.AddSingleton(configurationMapping);
        services.AddScoped<IMapper, ServiceMapper>();

        // Open/Closed : tout nouveau widget de Core est découvert sans modifier ce code.
        var typesWidgets = assemblage.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IWidgetTableauDeBord).IsAssignableFrom(type));
        foreach (var typeWidget in typesWidgets)
        {
            services.AddScoped(typeof(IWidgetTableauDeBord), typeWidget);
        }

        return services;
    }
}
