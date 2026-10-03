using erpWeb.Core.Echantillons;
using Microsoft.Extensions.Options;

namespace erpWeb.Core.Factures;

/// <summary>
/// Calcul d'une facture à partir d'un échantillon (sans accès aux données ; le numéro est attribué à l'enregistrement).
/// <list type="number">
/// <item>Montant brut = tarif de la filière (tarif par défaut si la filière est inconnue).</item>
/// <item>Montant hors taxe = brut × (1 − remise / 100), arrondi à 2 décimales.</item>
/// <item>TVA = montant hors taxe × taux / 100, arrondie à 2 décimales ; TTC = hors taxe + TVA (somme exacte).</item>
/// <item>Date de facture = validation du résultat, à défaut prélèvement ; échéance = date + délai.</item>
/// <item>Statut : en retard si l'échéance est déjà dépassée, émise sinon.</item>
/// </list>
/// </summary>
public sealed class CalculateurFacture : ICalculateurFacture
{
    private const int NombreDecimales = 2;

    private readonly OptionsFacturation _options;
    private readonly TimeProvider _horloge;

    public CalculateurFacture(IOptions<OptionsFacturation> options, TimeProvider horloge)
    {
        _options = options.Value;
        _horloge = horloge;
    }

    public Facture Calculer(Echantillon echantillon)
    {
        var client = echantillon.Client
            ?? throw new ArgumentException("L'échantillon doit être rattaché à un client pour être facturé.", nameof(echantillon));

        var tarif = _options.TarifsParFiliere.TryGetValue(echantillon.Filiere, out var tarifFiliere)
            ? tarifFiliere
            : _options.TarifParDefaut;

        var remise = _options.RemisePourcentage is > 0 ? _options.RemisePourcentage : null;
        var montantHorsTaxe = Arrondir(tarif * (1 - (remise ?? 0) / 100m));
        var montantTva = Arrondir(montantHorsTaxe * _options.TauxTva / 100m);

        var dateFacture = DateOnly.FromDateTime(echantillon.ResultatAgronomie?.DateValidation ?? echantillon.DatePrelevement);
        var dateEcheance = dateFacture.AddDays(_options.DelaiEcheanceEnJours);
        var aujourdhui = DateOnly.FromDateTime(_horloge.GetUtcNow().UtcDateTime);

        return new Facture
        {
            IdEchantillon = echantillon.Id,
            IdClient = client.Id,
            DateFacture = dateFacture,
            DateEcheance = dateEcheance,
            NomClient = Tronquer(client.RaisonSociale, LongueursFacture.NomClient)!,
            AdresseFacturation = Tronquer(AssemblerAdresse(client.Adresse1, client.Adresse2), LongueursFacture.AdresseFacturation),
            CodePostal = Tronquer(client.CodePostal, LongueursFacture.CodePostal),
            Ville = Tronquer(client.Ville, LongueursFacture.Ville),
            Pays = Tronquer(client.Pays, LongueursFacture.Pays),
            ModePaiement = Tronquer(_options.ModePaiementParDefaut, LongueursFacture.ModePaiement),
            MontantHorsTaxe = montantHorsTaxe,
            TauxTva = _options.TauxTva,
            MontantTva = montantTva,
            MontantToutesTaxesComprises = montantHorsTaxe + montantTva,
            Remise = remise,
            Devise = _options.Devise,
            StatutFacture = dateEcheance < aujourdhui ? StatutsFacture.EnRetard : StatutsFacture.Emise,
        };
    }

    private static decimal Arrondir(decimal montant) => Math.Round(montant, NombreDecimales, MidpointRounding.AwayFromZero);

    private static string? AssemblerAdresse(string? ligne1, string? ligne2)
    {
        var lignes = new[] { ligne1, ligne2 }.Where(ligne => !string.IsNullOrWhiteSpace(ligne)).Select(ligne => ligne!.Trim());
        var adresse = string.Join(", ", lignes);
        return adresse.Length == 0 ? null : adresse;
    }

    private static string? Tronquer(string? valeur, int longueurMaximale)
        => valeur is { Length: > 0 } && valeur.Length > longueurMaximale ? valeur[..longueurMaximale] : valeur;
}
