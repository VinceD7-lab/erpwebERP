using Microsoft.AspNetCore.Identity;

namespace erpWeb.Core.Utilisateurs;

public class Utilisateur : IdentityUser
{
    public string NomComplet { get; set; } = string.Empty;

    public bool EstActif { get; set; } = true;

    public DateTime DateCreation { get; set; }
}
