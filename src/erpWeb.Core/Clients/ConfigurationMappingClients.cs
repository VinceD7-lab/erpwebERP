using Mapster;

namespace erpWeb.Core.Clients;

public sealed class ConfigurationMappingClients : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Client, ClientDto>();
        config.NewConfig<Client, ModificationClientDto>();
        config.NewConfig<CreationClientDto, Client>();

        // Garde-fou : l'entité est chargée par cet identifiant, la saisie ne doit jamais le réécrire.
        config.NewConfig<ModificationClientDto, Client>()
            .Ignore(client => client.Id);
    }
}
