using Mapster;

namespace erpWeb.Core.Clients;

public sealed class ConfigurationMappingClients : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Client, ClientDto>();
        config.NewConfig<Client, ModificationClientDto>();

        config.NewConfig<CreationClientDto, Client>()
            .Map(client => client.Telephone1, creation => NormalisationTelephone.Normaliser(creation.Telephone1))
            .Map(client => client.Telephone2, creation => NormalisationTelephone.Normaliser(creation.Telephone2));

        // Garde-fou : l'entité est chargée par cet identifiant, la saisie ne doit jamais le réécrire.
        config.NewConfig<ModificationClientDto, Client>()
            .Ignore(client => client.Id)
            .Map(client => client.Telephone1, modification => NormalisationTelephone.Normaliser(modification.Telephone1))
            .Map(client => client.Telephone2, modification => NormalisationTelephone.Normaliser(modification.Telephone2));
    }
}
