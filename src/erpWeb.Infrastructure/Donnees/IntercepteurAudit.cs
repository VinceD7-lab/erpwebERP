using System.Runtime.CompilerServices;
using System.Text.Json;
using erpWeb.Core.Audit;
using erpWeb.Core.Communs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace erpWeb.Infrastructure.Donnees;

/// <summary>
/// Alimente les champs d'EntiteAuditable et trace créations, modifications et suppressions
/// dans le journal d'audit. Les identifiants générés par la base n'étant connus qu'après
/// l'enregistrement, les entrées sont finalisées dans SavedChanges puis enregistrées à part.
/// </summary>
public sealed class IntercepteurAudit : SaveChangesInterceptor
{
    private const string UtilisateurSysteme = "système";

    private static readonly HashSet<string> _proprietesExclues =
    [
        nameof(IdentityUser.PasswordHash),
        nameof(IdentityUser.SecurityStamp),
        nameof(IdentityUser.ConcurrencyStamp),
    ];

    private static readonly JsonSerializerOptions _optionsJson = new() { WriteIndented = false };

    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly TimeProvider _horloge;
    private readonly ConditionalWeakTable<DbContext, List<EntreeEnAttente>> _entreesEnAttente = [];

    public IntercepteurAudit(IUtilisateurCourant utilisateurCourant, TimeProvider horloge)
    {
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PreparerAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PreparerAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (FinaliserAudit(eventData.Context))
        {
            eventData.Context!.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (FinaliserAudit(eventData.Context))
        {
            await eventData.Context!.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        AbandonnerAudit(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        AbandonnerAudit(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void PreparerAudit(DbContext? contexte)
    {
        if (contexte is null)
        {
            return;
        }

        var maintenant = _horloge.GetUtcNow().UtcDateTime;
        var utilisateur = _utilisateurCourant.NomUtilisateur ?? UtilisateurSysteme;
        var entrees = new List<EntreeEnAttente>();

        foreach (var entree in contexte.ChangeTracker.Entries().ToList())
        {
            if (!EstAuditable(entree))
            {
                continue;
            }

            if (entree.Entity is EntiteAuditable auditable)
            {
                RenseignerChampsAudit(entree, auditable, maintenant, utilisateur);
            }

            var journal = entree.State switch
            {
                EntityState.Added => CreerJournal(entree, ActionAudit.Creation, maintenant, utilisateur),
                EntityState.Modified => CreerJournalModification(entree, maintenant, utilisateur),
                EntityState.Deleted => CreerJournalSuppression(entree, maintenant, utilisateur),
                _ => null,
            };

            if (journal is not null)
            {
                entrees.Add(new EntreeEnAttente(entree, journal));
            }
        }

        _entreesEnAttente.AddOrUpdate(contexte, entrees);
    }

    private bool FinaliserAudit(DbContext? contexte)
    {
        if (contexte is null || !_entreesEnAttente.TryGetValue(contexte, out var entrees))
        {
            return false;
        }

        _entreesEnAttente.Remove(contexte);
        if (entrees.Count == 0)
        {
            return false;
        }

        foreach (var (entree, journal) in entrees)
        {
            // Les clés générées par la base sont désormais connues.
            journal.IdEntite = LireCle(entree);
            if (journal.Action == ActionAudit.Creation)
            {
                journal.NouvellesValeurs = Serialiser(entree.Properties.Where(EstTracee).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
        }

        contexte.Set<EntreeJournalAudit>().AddRange(entrees.Select(entree => entree.Journal));
        return true;
    }

    private void AbandonnerAudit(DbContext? contexte)
    {
        if (contexte is not null)
        {
            _entreesEnAttente.Remove(contexte);
        }
    }

    private static bool EstAuditable(EntityEntry entree)
        => entree.Entity is not EntreeJournalAudit
            && entree.Entity is not IdentityUserToken<string>
            && entree.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private static void RenseignerChampsAudit(EntityEntry entree, EntiteAuditable auditable, DateTime maintenant, string utilisateur)
    {
        if (entree.State == EntityState.Added)
        {
            auditable.DateCreation = maintenant;
            auditable.CreePar = utilisateur;
        }
        else if (entree.State == EntityState.Modified)
        {
            auditable.DateModification = maintenant;
            auditable.ModifiePar = utilisateur;
            entree.Property(nameof(EntiteAuditable.DateCreation)).IsModified = false;
            entree.Property(nameof(EntiteAuditable.CreePar)).IsModified = false;
        }
    }

    private static EntreeJournalAudit CreerJournal(EntityEntry entree, ActionAudit action, DateTime maintenant, string utilisateur)
        => new()
        {
            TypeEntite = entree.Metadata.ClrType.Name.Split('`')[0],
            Action = action,
            Utilisateur = utilisateur,
            Date = maintenant,
        };

    private static EntreeJournalAudit? CreerJournalModification(EntityEntry entree, DateTime maintenant, string utilisateur)
    {
        var modifiees = entree.Properties.Where(p => p.IsModified && EstTracee(p) && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
        if (modifiees.Count == 0)
        {
            return null;
        }

        var journal = CreerJournal(entree, ActionAudit.Modification, maintenant, utilisateur);
        journal.AnciennesValeurs = Serialiser(modifiees.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
        journal.NouvellesValeurs = Serialiser(modifiees.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
        return journal;
    }

    private static EntreeJournalAudit CreerJournalSuppression(EntityEntry entree, DateTime maintenant, string utilisateur)
    {
        var journal = CreerJournal(entree, ActionAudit.Suppression, maintenant, utilisateur);
        journal.AnciennesValeurs = Serialiser(entree.Properties.Where(EstTracee).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
        return journal;
    }

    private static bool EstTracee(PropertyEntry propriete) => !_proprietesExclues.Contains(propriete.Metadata.Name);

    private static string LireCle(EntityEntry entree)
    {
        var cle = entree.Metadata.FindPrimaryKey();
        return cle is null
            ? string.Empty
            : string.Join("|", cle.Properties.Select(p => entree.Property(p.Name).CurrentValue));
    }

    private static string Serialiser(Dictionary<string, object?> valeurs) => JsonSerializer.Serialize(valeurs, _optionsJson);

    private sealed record EntreeEnAttente(EntityEntry Entree, EntreeJournalAudit Journal);
}
