using System.Linq.Expressions;
using erpWeb.Core.Communs;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Audit;

/// <summary>
/// Pagination, filtrage et tri du journal d'audit côté serveur.
/// Les critères sont ramenés aux bornes plutôt que rejetés : contrairement à une commande,
/// une requête de lecture ne renvoie pas de <see cref="ResultatOperation"/> et ne doit pas
/// échouer parce qu'une grille demande une page qui n'existe plus.
/// </summary>
public sealed class ServiceRechercheJournalAudit : IRechercheJournalAudit
{
    private readonly IAppDbContext _contexte;
    private readonly IMapper _mapper;

    public ServiceRechercheJournalAudit(IAppDbContext contexte, IMapper mapper)
    {
        _contexte = contexte;
        _mapper = mapper;
    }

    public async Task<ResultatPagine<EntreeJournalAuditDto>> RechercherAsync(CriteresJournalAudit criteres, CancellationToken jetonAnnulation = default)
    {
        var numeroPage = Math.Max(criteres.NumeroPage, 1);
        var taillePage = Math.Clamp(criteres.TaillePage, 1, CriteresJournalAudit.TaillePageMaximum);

        var requete = Filtrer(_contexte.JournalAudit.AsNoTracking(), criteres);
        var nombreTotal = await requete.CountAsync(jetonAnnulation);

        var entrees = await Trier(requete, criteres)
            .Skip((numeroPage - 1) * taillePage)
            .Take(taillePage)
            .ToListAsync(jetonAnnulation);

        return new ResultatPagine<EntreeJournalAuditDto>(
            _mapper.Map<List<EntreeJournalAuditDto>>(entrees), numeroPage, taillePage, nombreTotal);
    }

    private static IQueryable<EntreeJournalAudit> Filtrer(IQueryable<EntreeJournalAudit> requete, CriteresJournalAudit criteres)
    {
        if (criteres.Action is { } action)
        {
            requete = requete.Where(entree => entree.Action == action);
        }

        if (criteres.DateDebut is { } dateDebut)
        {
            // Variable locale : EF ne traduit pas l'accès à .UtcDateTime dans l'arbre d'expression.
            var debut = dateDebut.UtcDateTime;
            requete = requete.Where(entree => entree.Date >= debut);
        }

        if (criteres.DateFin is { } dateFin)
        {
            var fin = dateFin.UtcDateTime;
            requete = requete.Where(entree => entree.Date < fin);
        }

        var recherche = criteres.Recherche?.Trim();
        if (!string.IsNullOrEmpty(recherche))
        {
            requete = requete.Where(entree =>
                (entree.Utilisateur != null && entree.Utilisateur.Contains(recherche))
                || entree.TypeEntite.Contains(recherche)
                || entree.IdEntite.Contains(recherche));
        }

        return requete;
    }

    private static IOrderedQueryable<EntreeJournalAudit> Trier(IQueryable<EntreeJournalAudit> requete, CriteresJournalAudit criteres)
    {
        // Action est persistée en chaîne (HasConversion<string>) : le tri est alphabétique
        // sur Creation / Modification / Suppression, et non sur l'ordre de l'énumération.
        var ordonnee = criteres.Tri switch
        {
            ChampTriJournalAudit.Utilisateur => Ordonner(requete, entree => entree.Utilisateur, criteres.TriDescendant),
            ChampTriJournalAudit.TypeEntite => Ordonner(requete, entree => entree.TypeEntite, criteres.TriDescendant),
            ChampTriJournalAudit.Action => Ordonner(requete, entree => entree.Action, criteres.TriDescendant),
            _ => Ordonner(requete, entree => entree.Date, criteres.TriDescendant),
        };

        // Départage indispensable : deux entrées peuvent partager la même date à la milliseconde,
        // et un ordre instable ferait réapparaître ou disparaître des lignes entre deux pages.
        return criteres.TriDescendant
            ? ordonnee.ThenByDescending(entree => entree.Id)
            : ordonnee.ThenBy(entree => entree.Id);
    }

    private static IOrderedQueryable<EntreeJournalAudit> Ordonner<TCle>(
        IQueryable<EntreeJournalAudit> requete,
        Expression<Func<EntreeJournalAudit, TCle>> cle,
        bool descendant)
        => descendant ? requete.OrderByDescending(cle) : requete.OrderBy(cle);
}
