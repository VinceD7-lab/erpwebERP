namespace erpWeb.Core.Audit;

public interface IExportJournalAudit
{
    string TypeContenu { get; }

    string ExtensionFichier { get; }

    byte[] Exporter(IReadOnlyList<EntreeJournalAuditDto> entrees);
}
