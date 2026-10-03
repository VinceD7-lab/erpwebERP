using System.Linq.Expressions;

namespace erpWeb.Core.Factures;

/// <summary>Projection partagée par la recherche et l'impression : une seule définition du contenu de <see cref="FactureDto"/>.</summary>
internal static class ProjectionFacture
{
    public static Expression<Func<Facture, FactureDto>> VersDto { get; } = facture => new FactureDto(
        facture.Id,
        facture.IdEchantillon,
        facture.Echantillon!.CodeBarresAnonyme,
        facture.Echantillon.Filiere,
        facture.NumeroFacture,
        facture.DateFacture,
        facture.DateEcheance,
        facture.IdClient,
        facture.NomClient,
        facture.AdresseFacturation,
        facture.CodePostal,
        facture.Ville,
        facture.Pays,
        facture.ModePaiement,
        facture.MontantHorsTaxe,
        facture.TauxTva,
        facture.MontantTva,
        facture.MontantToutesTaxesComprises,
        facture.Remise,
        facture.Devise,
        facture.StatutFacture,
        facture.DatePaiement,
        facture.Commentaire);
}
