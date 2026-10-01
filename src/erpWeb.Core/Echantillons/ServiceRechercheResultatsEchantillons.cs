using System.Linq.Expressions;
using erpWeb.Core.Communs;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Echantillons;

/// <summary>
/// Pagination, filtrage et tri des résultats d'échantillons côté serveur.
/// Les critères sont ramenés aux bornes plutôt que rejetés (voir <see cref="CriteresResultatsEchantillons"/>).
/// </summary>
public sealed class ServiceRechercheResultatsEchantillons : IRechercheResultatsEchantillons
{
    private readonly IAppDbContext _contexte;

    public ServiceRechercheResultatsEchantillons(IAppDbContext contexte)
    {
        _contexte = contexte;
    }

    public async Task<ResultatPagine<ResultatEchantillonDto>> RechercherAsync(CriteresResultatsEchantillons criteres, CancellationToken jetonAnnulation = default)
    {
        var taillePage = Math.Clamp(criteres.TaillePage, 1, CriteresResultatsEchantillons.TaillePageMaximum);

        // Borne haute : (numeroPage - 1) * taillePage doit tenir dans un int, sinon SQL Server refuse l'OFFSET.
        var numeroPage = Math.Clamp(criteres.NumeroPage, 1, int.MaxValue / taillePage);

        var requete = Filtrer(_contexte.Echantillons.AsNoTracking(), criteres);
        var nombreTotal = await requete.CountAsync(jetonAnnulation);

        var elements = await Trier(requete, criteres)
            .Skip((numeroPage - 1) * taillePage)
            .Take(taillePage)
            .Select(echantillon => new ResultatEchantillonDto(
                echantillon.Id,
                echantillon.Tournee != null ? echantillon.Tournee.DateTournee : null,
                echantillon.IdClient,
                echantillon.Client != null ? echantillon.Client.RaisonSociale : null,
                echantillon.CodeBarresAnonyme,
                echantillon.Filiere,
                echantillon.StatutAnalyse,
                echantillon.TemperatureReception,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.TypeSupport : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.PhSol : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.MatiereOrganique : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.PhosphoreP2O5 : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.PotassiumK2O : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.ReliquatAzoteN : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.ValeurUclFourrage : null,
                echantillon.ResultatAgronomie != null ? echantillon.ResultatAgronomie.DateValidation : null))
            .ToListAsync(jetonAnnulation);

        return new ResultatPagine<ResultatEchantillonDto>(elements, numeroPage, taillePage, nombreTotal);
    }

    private static IQueryable<Echantillon> Filtrer(IQueryable<Echantillon> requete, CriteresResultatsEchantillons criteres)
    {
        if (criteres.DateTourneeDebut is { } debut)
        {
            requete = requete.Where(echantillon => echantillon.Tournee != null && echantillon.Tournee.DateTournee >= debut);
        }

        if (criteres.DateTourneeFin is { } fin)
        {
            requete = requete.Where(echantillon => echantillon.Tournee != null && echantillon.Tournee.DateTournee <= fin);
        }

        if (criteres.IdClient is { } idClient)
        {
            requete = requete.Where(echantillon => echantillon.IdClient == idClient);
        }

        var statut = criteres.StatutAnalyse?.Trim();
        if (!string.IsNullOrEmpty(statut))
        {
            requete = requete.Where(echantillon => echantillon.StatutAnalyse == statut);
        }

        return requete;
    }

    private static IOrderedQueryable<Echantillon> Trier(IQueryable<Echantillon> requete, CriteresResultatsEchantillons criteres)
    {
        var descendant = criteres.TriDescendant;

        var ordonnee = criteres.Tri switch
        {
            ChampTriResultatsEchantillons.Client => Ordonner(requete, echantillon => echantillon.Client!.RaisonSociale, descendant),
            ChampTriResultatsEchantillons.CodeBarres => Ordonner(requete, echantillon => echantillon.CodeBarresAnonyme, descendant),
            ChampTriResultatsEchantillons.Filiere => Ordonner(requete, echantillon => echantillon.Filiere, descendant),
            ChampTriResultatsEchantillons.StatutAnalyse => Ordonner(requete, echantillon => echantillon.StatutAnalyse, descendant),
            _ => Ordonner(requete, echantillon => echantillon.Tournee!.DateTournee, descendant),
        };

        // Tri par défaut : date de tournée puis client, pour rendre contiguës les lignes d'un même client.
        if (criteres.Tri == ChampTriResultatsEchantillons.DateTournee)
        {
            ordonnee = descendant
                ? ordonnee.ThenByDescending(echantillon => echantillon.Client!.RaisonSociale)
                : ordonnee.ThenBy(echantillon => echantillon.Client!.RaisonSociale);
        }

        // Départage indispensable : un ordre instable ferait réapparaître ou disparaître des lignes entre deux pages.
        return descendant
            ? ordonnee.ThenByDescending(echantillon => echantillon.Id)
            : ordonnee.ThenBy(echantillon => echantillon.Id);
    }

    private static IOrderedQueryable<Echantillon> Ordonner<TCle>(
        IQueryable<Echantillon> requete,
        Expression<Func<Echantillon, TCle>> cle,
        bool descendant)
        => descendant ? requete.OrderByDescending(cle) : requete.OrderBy(cle);
}
