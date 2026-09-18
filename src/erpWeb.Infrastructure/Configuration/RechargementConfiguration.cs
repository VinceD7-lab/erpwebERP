using erpWeb.Core.Parametres;
using Microsoft.Extensions.Configuration;

namespace erpWeb.Infrastructure.Configuration;

public sealed class RechargementConfiguration : IRechargementConfiguration
{
    private readonly IConfiguration _configuration;

    public RechargementConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Recharger()
    {
        if (_configuration is IConfigurationRoot racine)
        {
            racine.Reload();
        }
    }
}
