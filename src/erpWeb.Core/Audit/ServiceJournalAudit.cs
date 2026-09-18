using erpWeb.Core.Communs;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Audit;

public sealed class ServiceJournalAudit : ILectureJournalAudit
{
    private readonly IAppDbContext _contexte;
    private readonly IMapper _mapper;
    private readonly TimeProvider _horloge;

    public ServiceJournalAudit(IAppDbContext contexte, IMapper mapper, TimeProvider horloge)
    {
        _contexte = contexte;
        _mapper = mapper;
        _horloge = horloge;
    }

    public async Task<IReadOnlyList<EntreeJournalAuditDto>> ListerRecentesAsync(int nombreMaximum, CancellationToken jetonAnnulation = default)
    {
        var entrees = await _contexte.JournalAudit
            .AsNoTracking()
            .OrderByDescending(entree => entree.Date)
            .ThenByDescending(entree => entree.Id)
            .Take(nombreMaximum)
            .ToListAsync(jetonAnnulation);

        return _mapper.Map<List<EntreeJournalAuditDto>>(entrees);
    }

    public async Task<IReadOnlyList<ActiviteJournaliere>> ObtenirActiviteAsync(int nombreJours, CancellationToken jetonAnnulation = default)
    {
        var aujourdhui = DateOnly.FromDateTime(_horloge.GetUtcNow().UtcDateTime);
        var premierJour = aujourdhui.AddDays(1 - nombreJours);
        var debut = premierJour.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var dates = await _contexte.JournalAudit
            .AsNoTracking()
            .Where(entree => entree.Date >= debut)
            .Select(entree => entree.Date)
            .ToListAsync(jetonAnnulation);

        var operationsParJour = dates
            .GroupBy(DateOnly.FromDateTime)
            .ToDictionary(groupe => groupe.Key, groupe => groupe.Count());

        return Enumerable.Range(0, nombreJours)
            .Select(decalage => premierJour.AddDays(decalage))
            .Select(jour => new ActiviteJournaliere(jour, operationsParJour.GetValueOrDefault(jour)))
            .ToList();
    }
}
