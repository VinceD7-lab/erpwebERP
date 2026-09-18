using Mapster;

namespace erpWeb.Core.Audit;

public sealed class ConfigurationMappingAudit : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EntreeJournalAudit, EntreeJournalAuditDto>();
    }
}
