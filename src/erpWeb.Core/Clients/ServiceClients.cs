using erpWeb.Core.Communs;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Clients;

public sealed class ServiceClients : IServiceClients
{
    private const string MessageIntrouvable = "Client introuvable.";

    private readonly IAppDbContext _contexte;
    private readonly IValidator<CreationClientDto> _validateurCreation;
    private readonly IValidator<ModificationClientDto> _validateurModification;
    private readonly IMapper _mapper;

    public ServiceClients(
        IAppDbContext contexte,
        IValidator<CreationClientDto> validateurCreation,
        IValidator<ModificationClientDto> validateurModification,
        IMapper mapper)
    {
        _contexte = contexte;
        _validateurCreation = validateurCreation;
        _validateurModification = validateurModification;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ClientDto>> ListerAsync(CancellationToken jetonAnnulation = default)
    {
        var clients = await _contexte.Clients
            .AsNoTracking()
            .OrderBy(client => client.RaisonSociale)
            .ToListAsync(jetonAnnulation);

        return _mapper.Map<List<ClientDto>>(clients);
    }

    public async Task<ModificationClientDto?> ObtenirPourModificationAsync(int id, CancellationToken jetonAnnulation = default)
    {
        var client = await _contexte.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.Id == id, jetonAnnulation);

        return client is null ? null : _mapper.Map<ModificationClientDto>(client);
    }

    public async Task<ResultatOperation<int>> CreerAsync(CreationClientDto creation, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurCreation.ValidateAsync(creation, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation<int>.Echec(validation.MessagesErreur());
        }

        var client = _mapper.Map<Client>(creation);
        _contexte.Clients.Add(client);
        await _contexte.SaveChangesAsync(jetonAnnulation);
        return ResultatOperation<int>.Succes(client.Id);
    }

    public async Task<ResultatOperation> ModifierAsync(ModificationClientDto modification, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurModification.ValidateAsync(modification, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation.Echec(validation.MessagesErreur());
        }

        var client = await _contexte.Clients.FirstOrDefaultAsync(client => client.Id == modification.Id, jetonAnnulation);
        if (client is null)
        {
            return ResultatOperation.Echec(MessageIntrouvable);
        }

        _mapper.Map(modification, client);
        await _contexte.SaveChangesAsync(jetonAnnulation);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation> SupprimerAsync(int id, CancellationToken jetonAnnulation = default)
    {
        var client = await _contexte.Clients.FirstOrDefaultAsync(client => client.Id == id, jetonAnnulation);
        if (client is null)
        {
            return ResultatOperation.Echec(MessageIntrouvable);
        }

        _contexte.Clients.Remove(client);
        await _contexte.SaveChangesAsync(jetonAnnulation);
        return ResultatOperation.Succes();
    }
}
