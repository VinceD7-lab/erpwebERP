using System.Net;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Email;
using FluentValidation;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.Core.Utilisateurs;

public sealed class ServiceUtilisateurs : IServiceUtilisateurs
{
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly RoleManager<IdentityRole> _gestionnaireRoles;
    private readonly IValidator<CreationUtilisateurDto> _validateurCreation;
    private readonly IValidator<ModificationUtilisateurDto> _validateurModification;
    private readonly IValidator<InscriptionDto> _validateurInscription;
    private readonly IValidator<ReinitialisationMotDePasseDto> _validateurReinitialisation;
    private readonly IServiceEmail _serviceEmail;
    private readonly IMapper _mapper;
    private readonly TimeProvider _horloge;

    public ServiceUtilisateurs(
        UserManager<Utilisateur> gestionnaireUtilisateurs,
        RoleManager<IdentityRole> gestionnaireRoles,
        IValidator<CreationUtilisateurDto> validateurCreation,
        IValidator<ModificationUtilisateurDto> validateurModification,
        IValidator<InscriptionDto> validateurInscription,
        IValidator<ReinitialisationMotDePasseDto> validateurReinitialisation,
        IServiceEmail serviceEmail,
        IMapper mapper,
        TimeProvider horloge)
    {
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _gestionnaireRoles = gestionnaireRoles;
        _validateurCreation = validateurCreation;
        _validateurModification = validateurModification;
        _validateurInscription = validateurInscription;
        _validateurReinitialisation = validateurReinitialisation;
        _serviceEmail = serviceEmail;
        _mapper = mapper;
        _horloge = horloge;
    }

    public async Task<IReadOnlyList<UtilisateurDto>> ListerAsync(CancellationToken jetonAnnulation = default)
    {
        var utilisateurs = await _gestionnaireUtilisateurs.Users
            .AsNoTracking()
            .OrderBy(utilisateur => utilisateur.NomComplet)
            .ToListAsync(jetonAnnulation);

        var resultat = new List<UtilisateurDto>(utilisateurs.Count);
        foreach (var utilisateur in utilisateurs)
        {
            resultat.Add(await VersDtoAsync(utilisateur));
        }

        return resultat;
    }

    public async Task<UtilisateurDto?> ObtenirAsync(string id, CancellationToken jetonAnnulation = default)
    {
        var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(id);
        return utilisateur is null ? null : await VersDtoAsync(utilisateur);
    }

    public async Task<IReadOnlyList<string>> ListerRolesAsync(CancellationToken jetonAnnulation = default)
        => await _gestionnaireRoles.Roles
            .Select(role => role.Name!)
            .OrderBy(nom => nom)
            .ToListAsync(jetonAnnulation);

    public async Task<ResultatOperation<string>> CreerAsync(CreationUtilisateurDto creation, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurCreation.ValidateAsync(creation, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation<string>.Echec(validation.MessagesErreur());
        }

        var resultat = await CreerCompteAsync(creation.Email, creation.NomComplet, creation.MotDePasse, creation.Roles, jetonAnnulation);
        if (resultat.Reussi)
        {
            var message = new MessageEmail(
                creation.Email,
                "Votre compte erpWeb a été créé",
                $"<p>Bonjour {WebUtility.HtmlEncode(creation.NomComplet)},</p><p>Votre compte erpWeb a été créé par un administrateur.</p>");
            await _serviceEmail.EnvoyerAsync(message, jetonAnnulation);
        }

        return resultat;
    }

    public async Task<ResultatOperation<string>> InscrireAsync(InscriptionDto inscription, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurInscription.ValidateAsync(inscription, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation<string>.Echec(validation.MessagesErreur());
        }

        return await CreerCompteAsync(inscription.Email, inscription.NomComplet, inscription.MotDePasse, [RolesApplication.Utilisateur], jetonAnnulation);
    }

    public async Task<ResultatOperation> ModifierAsync(ModificationUtilisateurDto modification, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurModification.ValidateAsync(modification, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation.Echec(validation.MessagesErreur());
        }

        var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(modification.Id);
        if (utilisateur is null)
        {
            return ResultatOperation.Echec("Utilisateur introuvable.");
        }

        var rolesInconnus = await ListerRolesInconnusAsync(modification.Roles, jetonAnnulation);
        if (rolesInconnus.Count > 0)
        {
            return ResultatOperation.Echec($"Rôles inconnus : {string.Join(", ", rolesInconnus)}.");
        }

        utilisateur.Email = modification.Email;
        utilisateur.UserName = modification.Email;
        utilisateur.NomComplet = modification.NomComplet;
        var miseAJour = await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
        if (!miseAJour.Succeeded)
        {
            return ResultatOperation.Echec(miseAJour.MessagesErreur());
        }

        var rolesActuels = await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur);
        var rolesARetirer = rolesActuels.Except(modification.Roles).ToList();
        var rolesAAjouter = modification.Roles.Except(rolesActuels).Distinct().ToList();

        if (rolesARetirer.Count > 0)
        {
            var retrait = await _gestionnaireUtilisateurs.RemoveFromRolesAsync(utilisateur, rolesARetirer);
            if (!retrait.Succeeded)
            {
                return ResultatOperation.Echec(retrait.MessagesErreur());
            }
        }

        if (rolesAAjouter.Count > 0)
        {
            var ajout = await _gestionnaireUtilisateurs.AddToRolesAsync(utilisateur, rolesAAjouter);
            if (!ajout.Succeeded)
            {
                return ResultatOperation.Echec(ajout.MessagesErreur());
            }
        }

        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation> DefinirActivationAsync(string id, bool estActif, CancellationToken jetonAnnulation = default)
    {
        var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(id);
        if (utilisateur is null)
        {
            return ResultatOperation.Echec("Utilisateur introuvable.");
        }

        utilisateur.EstActif = estActif;
        var miseAJour = await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
        if (!miseAJour.Succeeded)
        {
            return ResultatOperation.Echec(miseAJour.MessagesErreur());
        }

        // Invalide les cookies existants : un compte désactivé est déconnecté.
        await _gestionnaireUtilisateurs.UpdateSecurityStampAsync(utilisateur);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation> ReinitialiserMotDePasseAsync(ReinitialisationMotDePasseDto reinitialisation, CancellationToken jetonAnnulation = default)
    {
        var validation = await _validateurReinitialisation.ValidateAsync(reinitialisation, jetonAnnulation);
        if (!validation.IsValid)
        {
            return ResultatOperation.Echec(validation.MessagesErreur());
        }

        var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(reinitialisation.Id);
        if (utilisateur is null)
        {
            return ResultatOperation.Echec("Utilisateur introuvable.");
        }

        var jeton = await _gestionnaireUtilisateurs.GeneratePasswordResetTokenAsync(utilisateur);
        var resultat = await _gestionnaireUtilisateurs.ResetPasswordAsync(utilisateur, jeton, reinitialisation.NouveauMotDePasse);
        return resultat.Succeeded ? ResultatOperation.Succes() : ResultatOperation.Echec(resultat.MessagesErreur());
    }

    private async Task<ResultatOperation<string>> CreerCompteAsync(
        string email,
        string nomComplet,
        string motDePasse,
        IReadOnlyCollection<string> roles,
        CancellationToken jetonAnnulation)
    {
        var rolesInconnus = await ListerRolesInconnusAsync(roles, jetonAnnulation);
        if (rolesInconnus.Count > 0)
        {
            return ResultatOperation<string>.Echec($"Rôles inconnus : {string.Join(", ", rolesInconnus)}.");
        }

        var utilisateur = new Utilisateur
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomComplet = nomComplet,
            EstActif = true,
            DateCreation = _horloge.GetUtcNow().UtcDateTime,
        };

        var creation = await _gestionnaireUtilisateurs.CreateAsync(utilisateur, motDePasse);
        if (!creation.Succeeded)
        {
            return ResultatOperation<string>.Echec(creation.MessagesErreur());
        }

        if (roles.Count > 0)
        {
            var ajout = await _gestionnaireUtilisateurs.AddToRolesAsync(utilisateur, roles.Distinct());
            if (!ajout.Succeeded)
            {
                return ResultatOperation<string>.Echec(ajout.MessagesErreur());
            }
        }

        return ResultatOperation<string>.Succes(utilisateur.Id);
    }

    private async Task<IReadOnlyList<string>> ListerRolesInconnusAsync(IEnumerable<string> roles, CancellationToken jetonAnnulation)
    {
        var rolesExistants = await ListerRolesAsync(jetonAnnulation);
        return roles.Except(rolesExistants).ToList();
    }

    private async Task<UtilisateurDto> VersDtoAsync(Utilisateur utilisateur)
    {
        var dto = _mapper.Map<UtilisateurDto>(utilisateur);
        dto.Roles = (await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur)).Order().ToList();
        return dto;
    }
}
