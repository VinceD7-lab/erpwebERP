using System.Linq.Expressions;
using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.DevisClients;

/// <summary>
/// Pagination, recherche et tri des devis côté serveur.
/// Les critères sont ramenés aux bornes plutôt que rejetés (voir <see cref="CriteresDevis"/>).
/// </summary>
public sealed class ServiceRechercheDevis : IRechercheDevis
{
    private readonly IAppDbContext _contexte;

    public ServiceRechercheDevis(IAppDbContext contexte)
    {
        _contexte = contexte;
    }

    public async Task<ResultatPagine<DevisDto>> RechercherAsync(CriteresDevis criteres, CancellationToken jetonAnnulation = default)
    {
        var taillePage = Math.Clamp(criteres.TaillePage, 1, CriteresDevis.TaillePageMaximum);

        // Borne haute : (numeroPage - 1) * taillePage doit tenir dans un int, sinon SQL Server refuse l'OFFSET.
        var numeroPage = Math.Clamp(criteres.NumeroPage, 1, int.MaxValue / taillePage);

        var requete = Filtrer(_contexte.Devis.AsNoTracking(), criteres);
        var nombreTotal = await requete.CountAsync(jetonAnnulation);

        var elements = await Trier(requete, criteres)
            .Skip((numeroPage - 1) * taillePage)
            .Take(taillePage)
            .Select(devis => new DevisDto(
                devis.Id,
                devis.Numero,
                devis.ClientId,
                devis.Client!.RaisonSociale,
                devis.DateDevis,
                devis.DateValidite,
                devis.Statut,
                devis.Objet,
                devis.Reference,
                devis.Devise,
                devis.MontantTotalHorsTaxe,
                devis.MontantTotalTaxe,
                devis.MontantTotalToutesTaxesComprises))
            .ToListAsync(jetonAnnulation);

        return new ResultatPagine<DevisDto>(elements, numeroPage, taillePage, nombreTotal);
    }

    private static IQueryable<Devis> Filtrer(IQueryable<Devis> requete, CriteresDevis criteres)
    {
        var recherche = criteres.Recherche?.Trim();
        if (string.IsNullOrEmpty(recherche))
        {
            return requete;
        }

        if (recherche.Length > LongueursDevis.Recherche)
        {
            recherche = recherche[..LongueursDevis.Recherche];
        }

        // Contains : EF échappe les caractères génériques de LIKE (% et _) saisis par l'utilisateur.
        return requete.Where(devis =>
            devis.Numero.Contains(recherche)
            || devis.Client!.RaisonSociale.Contains(recherche)
            || devis.Objet.Contains(recherche)
            || (devis.Reference != null && devis.Reference.Contains(recherche)));
    }

    private static IOrderedQueryable<Devis> Trier(IQueryable<Devis> requete, CriteresDevis criteres)
    {
        var descendant = criteres.TriDescendant;

        var ordonnee = criteres.Tri switch
        {
            ChampTriDevis.Numero => Ordonner(requete, devis => devis.Numero, descendant),
            ChampTriDevis.DateDevis => Ordonner(requete, devis => devis.DateDevis, descendant),
            ChampTriDevis.DateValidite => Ordonner(requete, devis => devis.DateValidite, descendant),
            ChampTriDevis.Statut => Ordonner(requete, devis => devis.Statut, descendant),
            ChampTriDevis.Objet => Ordonner(requete, devis => devis.Objet, descendant),
            ChampTriDevis.MontantTotalToutesTaxesComprises => Ordonner(requete, devis => devis.MontantTotalToutesTaxesComprises, descendant),
            _ => Ordonner(requete, devis => devis.Client!.RaisonSociale, descendant),
        };

        // Départage indispensable : un ordre instable ferait réapparaître ou disparaître des lignes entre deux pages.
        // Au sein d'un même client, les devis les plus récents d'abord.
        if (criteres.Tri == ChampTriDevis.Client)
        {
            return ordonnee.ThenByDescending(devis => devis.DateDevis).ThenBy(devis => devis.Id);
        }

        return descendant
            ? ordonnee.ThenByDescending(devis => devis.Id)
            : ordonnee.ThenBy(devis => devis.Id);
    }

    private static IOrderedQueryable<Devis> Ordonner<TCle>(
        IQueryable<Devis> requete,
        Expression<Func<Devis, TCle>> cle,
        bool descendant)
        => descendant ? requete.OrderByDescending(cle) : requete.OrderBy(cle);
}
