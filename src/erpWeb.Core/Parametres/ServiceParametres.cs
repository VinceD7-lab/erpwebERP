using erpWeb.Core.Communs;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Parametres;

public sealed class ServiceParametres : IServiceParametres
{
    private readonly IAppDbContext _contexte;
    private readonly IValidator<ModificationParametreDto> _validateurModification;
    private readonly IRechargementConfiguration _rechargementConfiguration;
    private readonly IMapper _mapper;

    public ServiceParametres(
        IAppDbContext contexte,
        IValidator<ModificationParametreDto> validateurModification,
        IRechargementConfiguration rechargementConfiguration,
        IMapper mapper)
    {
        _contexte = contexte;
        _validateurModification = validateurModification;
        _rechargementConfiguration = rechargementConfiguration;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ParametreDto>> ListerAsync(CancellationToken jetonAnnulation = default)
    {
        var parametres = await _contexte.Parametres
            .AsNoTracking()
            .OrderBy(parametre => parametre.Cle)
            .ToListAsync(jetonAnnulation);

        return _mapper.Map<List<ParametreDto>>(parametres);
    }

    public async Task<ParametreDto?> ObtenirAsync(string cle, CancellationToken jetonAnnulation = default)
    {
        var parametre = await _contexte.Parametres
            .AsNoTracking()
            .FirstOrDefaultAsync(parametre => parametre.Cle == cle, jetonAnnulation);

        return parametre is null ? null : _mapper.Map<ParametreDto>(parametre);
    }

    public async Task<ResultatOperation> DefinirAsync(ModificationParametreDto modification, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurModification.ValidateAsync(modification, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation.Echec(validation.MessagesErreur());
        }

        var parametre = await _contexte.Parametres.FirstOrDefaultAsync(parametre => parametre.Cle == modification.Cle, jetonAnnulation);
        if (parametre is null)
        {
            parametre = new Parametre { Cle = modification.Cle };
            _contexte.Parametres.Add(parametre);
        }

        parametre.Valeur = modification.Valeur;
        parametre.Description = modification.Description;
        await _contexte.SaveChangesAsync(jetonAnnulation);

        _rechargementConfiguration.Recharger();
        return ResultatOperation.Succes();
    }
}
