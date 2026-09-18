using Microsoft.AspNetCore.Authorization;

namespace erpWeb.Web.Autorisation;

public sealed class ExigencePermission : IAuthorizationRequirement
{
    public ExigencePermission(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
