using erpWeb.Core.Autorisation;
using Microsoft.AspNetCore.Authorization;

namespace erpWeb.Web.Autorisation;

/// <summary>Accorde l'accès si l'un des rôles de l'utilisateur porte la permission (claim de rôle).</summary>
public sealed class GestionnairePermission : AuthorizationHandler<ExigencePermission>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExigencePermission requirement)
    {
        if (context.User.HasClaim(Permissions.TypeClaim, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
