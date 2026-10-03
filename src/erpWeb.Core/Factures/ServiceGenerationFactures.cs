using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Factures;

public sealed class ServiceGenerationFactures : IGenerationFactures
{
    private readonly IAppDbContext _contexte;
    private readonly ICalculateurFacture _calculateur;
    private readonly TimeProvider _horloge;

    public ServiceGenerationFactures(IAppDbContext contexte, ICalculateurFacture calculateur, TimeProvider horloge)
    {
        _contexte = contexte;
        _calculateur = calculateur;
        _horloge = horloge;
    }

    public async Task<int> GenererManquantesAsync(CancellationToken jetonAnnulation = default)
    {
        var echantillons = await _contexte.Echantillons
            .Include(echantillon => echantillon.Client)
            .Include(echantillon => echantillon.ResultatAgronomie)
            .Where(echantillon => echantillon.IdClient != null
                && !_contexte.Factures.Any(facture => facture.IdEchantillon == echantillon.Id))
            .OrderBy(echantillon => echantillon.DatePrelevement)
            .ThenBy(echantillon => echantillon.Id)
            .ToListAsync(jetonAnnulation);

        if (echantillons.Count > 0)
        {
            var numeroSuivant = (await _contexte.Factures.MaxAsync(facture => (int?)facture.NumeroFacture, jetonAnnulation) ?? 0) + 1;
            foreach (var echantillon in echantillons)
            {
                var facture = _calculateur.Calculer(echantillon);
                facture.NumeroFacture = numeroSuivant++;
                _contexte.Factures.Add(facture);
            }
        }

        await MarquerEnRetardAsync(jetonAnnulation);

        try
        {
            await _contexte.SaveChangesAsync(jetonAnnulation);
        }
        catch (DbUpdateException) when (echantillons.Count > 0)
        {
            // Une requête concurrente a généré les mêmes factures (index uniques) : elles existent, rien à faire.
            // Toute autre erreur (contrainte, troncature) remonte à l'appelant.
            if (!await DejaFactureesAsync(echantillons, jetonAnnulation))
            {
                throw;
            }

            return 0;
        }

        return echantillons.Count;
    }

    private async Task<bool> DejaFactureesAsync(List<Echantillon> echantillons, CancellationToken jetonAnnulation)
    {
        var identifiants = echantillons.Select(echantillon => echantillon.Id).ToList();
        return await _contexte.Factures
            .AsNoTracking()
            .AnyAsync(facture => identifiants.Contains(facture.IdEchantillon), jetonAnnulation);
    }

    private async Task MarquerEnRetardAsync(CancellationToken jetonAnnulation)
    {
        var aujourdhui = DateOnly.FromDateTime(_horloge.GetUtcNow().UtcDateTime);
        var enRetard = await _contexte.Factures
            .Where(facture => facture.StatutFacture == StatutsFacture.Emise && facture.DateEcheance < aujourdhui)
            .ToListAsync(jetonAnnulation);

        foreach (var facture in enRetard)
        {
            facture.StatutFacture = StatutsFacture.EnRetard;
        }
    }
}
