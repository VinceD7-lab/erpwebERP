using erpWeb.Core.Communs;

namespace erpWeb.Core.Utilisateurs;

public interface IServiceUtilisateurs
{
    Task<IReadOnlyList<UtilisateurDto>> ListerAsync(CancellationToken jetonAnnulation = default);

    Task<UtilisateurDto?> ObtenirAsync(string id, CancellationToken jetonAnnulation = default);

    Task<IReadOnlyList<string>> ListerRolesAsync(CancellationToken jetonAnnulation = default);

    Task<ResultatOperation<string>> CreerAsync(CreationUtilisateurDto creation, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation<string>> InscrireAsync(InscriptionDto inscription, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> ModifierAsync(ModificationUtilisateurDto modification, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> DefinirActivationAsync(string id, bool estActif, CancellationToken jetonAnnulation = default);

    Task<ResultatOperation> ReinitialiserMotDePasseAsync(ReinitialisationMotDePasseDto reinitialisation, CancellationToken jetonAnnulation = default);
}
