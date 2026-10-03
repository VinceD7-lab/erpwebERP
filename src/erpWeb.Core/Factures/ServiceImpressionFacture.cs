using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace erpWeb.Core.Factures;

public sealed class ServiceImpressionFacture : IServiceImpressionFacture
{
    private readonly IAppDbContext _contexte;
    private readonly OptionsFacturation _options;

    public ServiceImpressionFacture(IAppDbContext contexte, IOptions<OptionsFacturation> options)
    {
        _contexte = contexte;
        _options = options.Value;
    }

    public async Task<ImpressionFactureDto?> ObtenirAsync(int idFacture, CancellationToken jetonAnnulation = default)
    {
        var facture = await _contexte.Factures
            .AsNoTracking()
            .Where(candidate => candidate.Id == idFacture)
            .Select(ProjectionFacture.VersDto)
            .FirstOrDefaultAsync(jetonAnnulation);

        return facture is null ? null : new ImpressionFactureDto(facture, _options.NomEmetteur, _options.AdresseEmetteur);
    }
}
