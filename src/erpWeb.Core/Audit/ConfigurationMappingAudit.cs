using Mapster;

namespace erpWeb.Core.Audit;

public sealed class ConfigurationMappingAudit : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Les dates sont écrites en UTC mais relues avec un Kind non spécifié : sans cette précision,
        // ToLocalTime() dans une vue ne convertit rien et la sérialisation JSON omet le suffixe Z.
        config.NewConfig<EntreeJournalAudit, EntreeJournalAuditDto>()
            .Map(destination => destination.Date, source => DateTime.SpecifyKind(source.Date, DateTimeKind.Utc));
    }
}
