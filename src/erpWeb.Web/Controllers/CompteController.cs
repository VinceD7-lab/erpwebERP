using erpWeb.Core.Utilisateurs;
using erpWeb.Web.Modeles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace erpWeb.Web.Controllers;

[AllowAnonymous]
public sealed class CompteController : Controller
{
    private readonly SignInManager<Utilisateur> _gestionnaireConnexion;
    private readonly IServiceUtilisateurs _serviceUtilisateurs;

    public CompteController(SignInManager<Utilisateur> gestionnaireConnexion, IServiceUtilisateurs serviceUtilisateurs)
    {
        _gestionnaireConnexion = gestionnaireConnexion;
        _serviceUtilisateurs = serviceUtilisateurs;
    }

    [HttpGet]
    public IActionResult Connexion([FromQuery(Name = "ReturnUrl")] string? urlRetour = null)
        => View(new ConnexionViewModel { UrlRetour = urlRetour });

    [HttpPost]
    public async Task<IActionResult> Connexion(ConnexionViewModel connexion)
    {
        if (!ModelState.IsValid)
        {
            return View(connexion);
        }

        var resultat = await _gestionnaireConnexion.PasswordSignInAsync(connexion.Email, connexion.MotDePasse, connexion.SeSouvenir, lockoutOnFailure: true);
        if (resultat.Succeeded)
        {
            return LocalRedirect(Url.IsLocalUrl(connexion.UrlRetour) ? connexion.UrlRetour : Url.Content("~/"));
        }

        var message = resultat switch
        {
            { IsLockedOut: true } => "Compte temporairement verrouillé après plusieurs échecs. Réessayer plus tard.",
            { IsNotAllowed: true } => "Ce compte est désactivé.",
            _ => "Email ou mot de passe incorrect.",
        };
        ModelState.AddModelError(string.Empty, message);
        return View(connexion);
    }

    [HttpGet]
    public IActionResult Inscription() => View(new InscriptionDto());

    [HttpPost]
    public async Task<IActionResult> Inscription(InscriptionDto inscription, CancellationToken jetonAnnulation)
    {
        var resultat = await _serviceUtilisateurs.InscrireAsync(inscription, jetonAnnulation);
        if (!resultat.Reussi)
        {
            ModelState.AjouterErreurs(resultat.Erreurs);
            return View(inscription);
        }

        var utilisateur = await _gestionnaireConnexion.UserManager.FindByIdAsync(resultat.Valeur!);
        await _gestionnaireConnexion.SignInAsync(utilisateur!, isPersistent: false);
        TempData[CleMessages.Succes] = "Bienvenue ! Votre compte a été créé.";
        return RedirectToAction(nameof(TableauDeBordController.Index), "TableauDeBord");
    }

    [HttpPost]
    public async Task<IActionResult> Deconnexion()
    {
        await _gestionnaireConnexion.SignOutAsync();
        return RedirectToAction(nameof(Connexion));
    }

    [HttpGet]
    public IActionResult AccesRefuse() => View();
}
