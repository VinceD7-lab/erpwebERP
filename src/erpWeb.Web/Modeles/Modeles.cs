using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace erpWeb.Web.Modeles;

public static class CleMessages
{
    public const string Succes = "MessageSucces";
    public const string Erreur = "MessageErreur";
}

public static class ExtensionsModelState
{
    public static void AjouterErreurs(this ModelStateDictionary etatModele, IEnumerable<string> erreurs)
    {
        foreach (var erreur in erreurs)
        {
            etatModele.AddModelError(string.Empty, erreur);
        }
    }
}

public sealed class ConnexionViewModel
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "L'email est invalide.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string MotDePasse { get; set; } = string.Empty;

    [Display(Name = "Rester connecté")]
    public bool SeSouvenir { get; set; }

    public string? UrlRetour { get; set; }
}

public sealed class DepotDocumentViewModel
{
    [Required(ErrorMessage = "Le type d'entité est obligatoire.")]
    [Display(Name = "Type d'entité")]
    public string TypeEntite { get; set; } = "General";

    [Required(ErrorMessage = "L'identifiant d'entité est obligatoire.")]
    [Display(Name = "Identifiant d'entité")]
    public string IdEntite { get; set; } = "0";

    [Required(ErrorMessage = "Sélectionner un fichier.")]
    [Display(Name = "Fichier")]
    public IFormFile? Fichier { get; set; }
}

public sealed record SelectionRolesViewModel(IReadOnlyList<string> RolesDisponibles, IReadOnlyCollection<string> RolesSelectionnes);

public sealed record ErreurViewModel(string? IdentifiantRequete);
