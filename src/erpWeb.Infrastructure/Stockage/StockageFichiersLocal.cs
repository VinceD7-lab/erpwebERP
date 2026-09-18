using System.Text.RegularExpressions;
using erpWeb.Core.Documents;
using Microsoft.Extensions.Options;

namespace erpWeb.Infrastructure.Stockage;

/// <summary>
/// Stockage sur disque local. Les fichiers sont renommés (GUID) : le nom d'origine n'est conservé qu'en base,
/// ce qui empêche toute traversée de répertoire via le nom fourni par l'utilisateur.
/// </summary>
public sealed partial class StockageFichiersLocal : IStockageFichiers
{
    private const int TailleTampon = 81920;

    private readonly IOptions<OptionsStockage> _options;

    public StockageFichiersLocal(IOptions<OptionsStockage> options)
    {
        _options = options;
    }

    public async Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken jetonAnnulation = default)
    {
        var identifiant = Guid.NewGuid().ToString("N");
        var cheminStockage = $"{identifiant[..2]}/{identifiant}{ExtensionSure(nomFichier)}";
        var cheminComplet = ResoudreChemin(cheminStockage);

        Directory.CreateDirectory(Path.GetDirectoryName(cheminComplet)!);
        await using var fichier = new FileStream(cheminComplet, FileMode.CreateNew, FileAccess.Write, FileShare.None, TailleTampon, useAsync: true);
        await contenu.CopyToAsync(fichier, jetonAnnulation);

        return cheminStockage;
    }

    public Task<Stream> OuvrirAsync(string cheminStockage, CancellationToken jetonAnnulation = default)
    {
        Stream fichier = new FileStream(ResoudreChemin(cheminStockage), FileMode.Open, FileAccess.Read, FileShare.Read, TailleTampon, useAsync: true);
        return Task.FromResult(fichier);
    }

    public Task SupprimerAsync(string cheminStockage, CancellationToken jetonAnnulation = default)
    {
        var cheminComplet = ResoudreChemin(cheminStockage);
        if (File.Exists(cheminComplet))
        {
            File.Delete(cheminComplet);
        }

        return Task.CompletedTask;
    }

    private string ResoudreChemin(string cheminStockage)
    {
        var racine = Path.GetFullPath(_options.Value.Dossier);
        var cheminComplet = Path.GetFullPath(Path.Combine(racine, cheminStockage));

        if (!cheminComplet.StartsWith(racine + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chemin de stockage en dehors du dossier autorisé.");
        }

        return cheminComplet;
    }

    private static string ExtensionSure(string nomFichier)
    {
        var extension = Path.GetExtension(nomFichier);
        return ExpressionExtension().IsMatch(extension) ? extension.ToLowerInvariant() : string.Empty;
    }

    [GeneratedRegex(@"^\.[A-Za-z0-9]{1,10}$")]
    private static partial Regex ExpressionExtension();
}
