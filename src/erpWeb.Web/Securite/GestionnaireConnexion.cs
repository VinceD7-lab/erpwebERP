using erpWeb.Core.Utilisateurs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace erpWeb.Web.Securite;

/// <summary>Refuse la connexion des comptes désactivés.</summary>
public sealed class GestionnaireConnexion : SignInManager<Utilisateur>
{
    public GestionnaireConnexion(
        UserManager<Utilisateur> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<Utilisateur> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<Utilisateur>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<Utilisateur> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(Utilisateur user)
        => user.EstActif && await base.CanSignInAsync(user);
}
