namespace erpWeb.Core.Documents;

public sealed class OptionsStockage
{
    public const string Section = "Stockage";

    /// <summary>Dossier racine des fichiers, relatif au répertoire de l'application ou absolu.</summary>
    public string Dossier { get; set; } = "App_Data/documents";

    public long TailleMaximaleOctets { get; set; } = 10 * 1024 * 1024;
}
