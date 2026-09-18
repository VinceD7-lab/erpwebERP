using Mapster;

namespace erpWeb.Core.Documents;

public sealed class ConfigurationMappingDocuments : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Document, DocumentDto>();
    }
}
