using erpWeb.Core.Autorisation;
using erpWeb.Core.Utilisateurs;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[Authorize(Policy = Permissions.Utilisateurs.Lire)]
public sealed class UtilisateursController : Controller
{
    public const string CleRolesDisponibles = "RolesDisponibles";

    private readonly IServiceUtilisateurs _serviceUtilisateurs;

    public UtilisateursController(IServiceUtilisateurs serviceUtilisateurs)
    {
        _serviceUtilisateurs = serviceUtilisateurs;
    }

    public async Task<IActionResult> Index(CancellationToken jetonAnnulation)
        => View(await _serviceUtilisateurs.ListerAsync(jetonAnnulation));

    [HttpGet]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> Creer(CancellationToken jetonAnnulation)
    {
        await ChargerRolesAsync(jetonAnnulation);
        return View(new CreationUtilisateurDto { Roles = [RolesApplication.Utilisateur] });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> Creer(CreationUtilisateurDto creation, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceUtilisateurs.CreerAsync(creation, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            await ChargerRolesAsync(jetonAnnulation);
            return View(creation);
        }

        TempData[CleMessages.Succes] = $"L'utilisateur {creation.Email} a été créé.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> Modifier(string id, CancellationToken jetonAnnulation)
    {
        var utilisateur = await _serviceUtilisateurs.ObtenirAsync(id, jetonAnnulation);
        if (utilisateur is null)
        {
            return NotFound();
        }

        await ChargerRolesAsync(jetonAnnulation);
        return View(new ModificationUtilisateurDto
        {
            Id = utilisateur.Id,
            Email = utilisateur.Email,
            NomComplet = utilisateur.NomComplet,
            Roles = [.. utilisateur.Roles],
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> Modifier(ModificationUtilisateurDto modification, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceUtilisateurs.ModifierAsync(modification, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            await ChargerRolesAsync(jetonAnnulation);
            return View(modification);
        }

        TempData[CleMessages.Succes] = "Les modifications ont été enregistrées.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> DefinirActivation(string id, bool estActif, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceUtilisateurs.DefinirActivationAsync(id, estActif, jetonAnnulation);
        DefinirMessage(resultat.Reussi, estActif ? "Le compte a été réactivé." : "Le compte a été désactivé.", resultat.Erreurs);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Utilisateurs.Gerer)]
    public async Task<IActionResult> ReinitialiserMotDePasse(ReinitialisationMotDePasseDto reinitialisation, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceUtilisateurs.ReinitialiserMotDePasseAsync(reinitialisation, jetonAnnulation);
        DefinirMessage(resultat.Reussi, "Le mot de passe a été réinitialisé.", resultat.Erreurs);
        return RedirectToAction(nameof(Modifier), new { id = reinitialisation.Id });
    }

    private async Task ChargerRolesAsync(CancellationToken jetonAnnulation)
        => ViewData[CleRolesDisponibles] = await _serviceUtilisateurs.ListerRolesAsync(jetonAnnulation);

    private void DefinirMessage(bool reussi, string messageSucces, IEnumerable<string> erreurs)
    {
        if (reussi)
        {
            TempData[CleMessages.Succes] = messageSucces;
        }
        else
        {
            TempData[CleMessages.Erreur] = string.Join(" ", erreurs);
        }
    }
}
