using System.Linq.Expressions;
using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Factures;

/// <summary>
/// Pagination, filtrage et tri des factures côté serveur.
/// Les critères sont ramenés aux bornes plutôt que rejetés (voir <see cref="CriteresFactures"/>).
/// </summary>
public sealed class ServiceRechercheFactures : IRechercheFactures
{
    private readonly IAppDbContext _contexte;

    public ServiceRechercheFactures(IAppDbContext contexte)
    {
        _contexte = contexte;
    }

    public async Task<ResultatPagine<FactureDto>> RechercherAsync(CriteresFactures criteres, CancellationToken jetonAnnulation = default)
    {
        var taillePage = Math.Clamp(criteres.TaillePage, 1, CriteresFactures.TaillePageMaximum);

        // Borne haute : (numeroPage - 1) * taillePage doit tenir dans un int, sinon SQL Server refuse l'OFFSET.
        var numeroPage = Math.Clamp(criteres.NumeroPage, 1, int.MaxValue / taillePage);

        var requete = Filtrer(_contexte.Factures.AsNoTracking(), criteres);
        var nombreTotal = await requete.CountAsync(jetonAnnulation);

        var elements = await Trier(requete, criteres)
            .Skip((numeroPage - 1) * taillePage)
            .Take(taillePage)
            .Select(ProjectionFacture.VersDto)
            .ToListAsync(jetonAnnulation);

        return new ResultatPagine<FactureDto>(elements, numeroPage, taillePage, nombreTotal);
    }

    private static IQueryable<Facture> Filtrer(IQueryable<Facture> requete, CriteresFactures criteres)
    {
        if (criteres.DateFactureDebut is { } debut)
        {
            requete = requete.Where(facture => facture.DateFacture >= debut);
        }

        if (criteres.DateFactureFin is { } fin)
        {
            requete = requete.Where(facture => facture.DateFacture <= fin);
        }

        if (criteres.IdClient is { } idClient)
        {
            requete = requete.Where(facture => facture.IdClient == idClient);
        }

        var statut = criteres.StatutFacture?.Trim();
        if (!string.IsNullOrEmpty(statut))
        {
            requete = requete.Where(facture => facture.StatutFacture == statut);
        }

        return requete;
    }

    private static IOrderedQueryable<Facture> Trier(IQueryable<Facture> requete, CriteresFactures criteres)
    {
        var descendant = criteres.TriDescendant;

        var ordonnee = criteres.Tri switch
        {
            ChampTriFactures.NumeroFacture => Ordonner(requete, facture => facture.NumeroFacture, descendant),
            ChampTriFactures.Client => Ordonner(requete, facture => facture.NomClient, descendant),
            ChampTriFactures.DateEcheance => Ordonner(requete, facture => facture.DateEcheance, descendant),
            ChampTriFactures.MontantToutesTaxesComprises => Ordonner(requete, facture => facture.MontantToutesTaxesComprises, descendant),
            ChampTriFactures.StatutFacture => Ordonner(requete, facture => facture.StatutFacture, descendant),
            _ => Ordonner(requete, facture => facture.DateFacture, descendant),
        };

        // Départage indispensable : un ordre instable ferait réapparaître ou disparaître des lignes entre deux pages.
        return descendant
            ? ordonnee.ThenByDescending(facture => facture.Id)
            : ordonnee.ThenBy(facture => facture.Id);
    }

    private static IOrderedQueryable<Facture> Ordonner<TCle>(
        IQueryable<Facture> requete,
        Expression<Func<Facture, TCle>> cle,
        bool descendant)
        => descendant ? requete.OrderByDescending(cle) : requete.OrderBy(cle);
}
