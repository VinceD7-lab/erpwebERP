using Mapster;

namespace erpWeb.Core.Utilisateurs;

public sealed class ConfigurationMappingUtilisateurs : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Utilisateur, UtilisateurDto>()
            .Map(dto => dto.Email, utilisateur => utilisateur.Email ?? string.Empty)
            .Ignore(dto => dto.Roles);
    }
}
