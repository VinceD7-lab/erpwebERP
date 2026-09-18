using System.Security.Claims;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Utilisateurs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace erpWeb.Infrastructure.Donnees;

/// <summary>
/// Initialisation idempotente : migrations (optionnelles), rôles, claims de permissions
/// et compte administrateur.
/// </summary>
public sealed class InitialisateurDonnees
{
    private readonly AppDbContext _contexte;
    private readonly RoleManager<IdentityRole> _gestionnaireRoles;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly OptionsAdministrateur _optionsAdministrateur;
    private readonly TimeProvider _horloge;
    private readonly ILogger<InitialisateurDonnees> _journal;

    public InitialisateurDonnees(
        AppDbContext contexte,
        RoleManager<IdentityRole> gestionnaireRoles,
        UserManager<Utilisateur> gestionnaireUtilisateurs,
        IOptions<OptionsAdministrateur> optionsAdministrateur,
        TimeProvider horloge,
        ILogger<InitialisateurDonnees> journal)
    {
        _contexte = contexte;
        _gestionnaireRoles = gestionnaireRoles;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _optionsAdministrateur = optionsAdministrateur.Value;
        _horloge = horloge;
        _journal = journal;
    }

    public async Task InitialiserAsync(bool appliquerMigrations, CancellationToken jetonAnnulation = default)
    {
        if (appliquerMigrations)
        {
            await _contexte.Database.MigrateAsync(jetonAnnulation);
        }

        foreach (var (nomRole, permissions) in RolesApplication.PermissionsParRole)
        {
            await SynchroniserRoleAsync(nomRole, permissions);
        }

        await CreerAdministrateurAsync();
    }

    private async Task SynchroniserRoleAsync(string nomRole, IReadOnlyList<string> permissions)
    {
        var role = await _gestionnaireRoles.FindByNameAsync(nomRole);
        if (role is null)
        {
            role = new IdentityRole(nomRole);
            VerifierResultat(await _gestionnaireRoles.CreateAsync(role), $"création du rôle {nomRole}");
        }

        var claimsActuels = (await _gestionnaireRoles.GetClaimsAsync(role))
            .Where(claim => claim.Type == Permissions.TypeClaim)
            .ToList();

        foreach (var permission in permissions.Except(claimsActuels.Select(claim => claim.Value)))
        {
            VerifierResultat(
                await _gestionnaireRoles.AddClaimAsync(role, new Claim(Permissions.TypeClaim, permission)),
                $"ajout de la permission {permission} au rôle {nomRole}");
        }

        foreach (var claimObsolete in claimsActuels.Where(claim => !permissions.Contains(claim.Value)))
        {
            VerifierResultat(
                await _gestionnaireRoles.RemoveClaimAsync(role, claimObsolete),
                $"retrait de la permission {claimObsolete.Value} du rôle {nomRole}");
        }
    }

    private async Task CreerAdministrateurAsync()
    {
        if (await _gestionnaireUtilisateurs.FindByEmailAsync(_optionsAdministrateur.Email) is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_optionsAdministrateur.MotDePasse))
        {
            _journal.LogWarning(
                "Compte administrateur non créé : mot de passe absent. Exécuter : dotnet user-secrets set \"Administrateur:MotDePasse\" \"<mot de passe>\" --project src/erpWeb.Web");
            return;
        }

        var administrateur = new Utilisateur
        {
            UserName = _optionsAdministrateur.Email,
            Email = _optionsAdministrateur.Email,
            EmailConfirmed = true,
            NomComplet = _optionsAdministrateur.NomComplet,
            EstActif = true,
            DateCreation = _horloge.GetUtcNow().UtcDateTime,
        };

        VerifierResultat(
            await _gestionnaireUtilisateurs.CreateAsync(administrateur, _optionsAdministrateur.MotDePasse),
            "création du compte administrateur");
        VerifierResultat(
            await _gestionnaireUtilisateurs.AddToRoleAsync(administrateur, RolesApplication.Administrateur),
            "affectation du rôle administrateur");

        _journal.LogInformation("Compte administrateur {Email} créé.", administrateur.Email);
    }

    private static void VerifierResultat(IdentityResult resultat, string operation)
    {
        if (!resultat.Succeeded)
        {
            var erreurs = string.Join(" ", resultat.Errors.Select(erreur => erreur.Description));
            throw new InvalidOperationException($"Échec de l'initialisation ({operation}) : {erreurs}");
        }
    }
}
