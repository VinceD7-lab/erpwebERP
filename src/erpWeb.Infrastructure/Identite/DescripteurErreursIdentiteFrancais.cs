using Microsoft.AspNetCore.Identity;

namespace erpWeb.Infrastructure.Identite;

public sealed class DescripteurErreursIdentiteFrancais : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => Erreur(nameof(DefaultError), "Une erreur inconnue est survenue.");

    public override IdentityError DuplicateEmail(string email)
        => Erreur(nameof(DuplicateEmail), $"L'adresse {email} est déjà utilisée.");

    public override IdentityError DuplicateUserName(string userName)
        => Erreur(nameof(DuplicateUserName), $"L'identifiant {userName} est déjà utilisé.");

    public override IdentityError InvalidEmail(string? email)
        => Erreur(nameof(InvalidEmail), $"L'adresse {email} est invalide.");

    public override IdentityError InvalidUserName(string? userName)
        => Erreur(nameof(InvalidUserName), $"L'identifiant {userName} est invalide.");

    public override IdentityError InvalidToken()
        => Erreur(nameof(InvalidToken), "Le jeton est invalide ou a expiré.");

    public override IdentityError PasswordMismatch()
        => Erreur(nameof(PasswordMismatch), "Mot de passe incorrect.");

    public override IdentityError PasswordTooShort(int length)
        => Erreur(nameof(PasswordTooShort), $"Le mot de passe doit contenir au moins {length} caractères.");

    public override IdentityError PasswordRequiresDigit()
        => Erreur(nameof(PasswordRequiresDigit), "Le mot de passe doit contenir au moins un chiffre.");

    public override IdentityError PasswordRequiresLower()
        => Erreur(nameof(PasswordRequiresLower), "Le mot de passe doit contenir au moins une minuscule.");

    public override IdentityError PasswordRequiresUpper()
        => Erreur(nameof(PasswordRequiresUpper), "Le mot de passe doit contenir au moins une majuscule.");

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => Erreur(nameof(PasswordRequiresNonAlphanumeric), "Le mot de passe doit contenir au moins un caractère spécial.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => Erreur(nameof(PasswordRequiresUniqueChars), $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents.");

    public override IdentityError DuplicateRoleName(string role)
        => Erreur(nameof(DuplicateRoleName), $"Le rôle {role} existe déjà.");

    public override IdentityError InvalidRoleName(string? role)
        => Erreur(nameof(InvalidRoleName), $"Le nom de rôle {role} est invalide.");

    public override IdentityError UserAlreadyInRole(string role)
        => Erreur(nameof(UserAlreadyInRole), $"L'utilisateur possède déjà le rôle {role}.");

    public override IdentityError UserNotInRole(string role)
        => Erreur(nameof(UserNotInRole), $"L'utilisateur ne possède pas le rôle {role}.");

    private static IdentityError Erreur(string code, string description) => new() { Code = code, Description = description };
}
