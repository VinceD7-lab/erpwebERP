using Mapster;

namespace erpWeb.Core.Parametres;

public sealed class ConfigurationMappingParametres : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Parametre, ParametreDto>();
    }
}
