# erpWeb.Core — couche métier

Aucune dépendance vers Infrastructure ou Web : vérifié par `tests/erpWeb.UnitTests/Architecture/ArchitectureTests.cs`.

## Organisation par module

`Communs/`, `Autorisation/`, `Utilisateurs/`, `Audit/`, `Documents/`, `Parametres/`, `Email/`, `TableauDeBord/`. Un module contient :

- l'entité (`Document.cs`), les DTOs (`Dtos.cs`), les validateurs FluentValidation
- l'interface et le service (`IServiceDocuments`, `ServiceDocuments`)
- la configuration Mapster `ConfigurationMapping<Module> : IRegister`
- les options typées éventuelles (`OptionsStockage`, avec une constante `Section`)

Modèle de référence pour un nouveau module : `Documents/`.

## Règles

- **Données** : uniquement via `IAppDbContext` (DbSet + `SaveChangesAsync`). Une nouvelle entité ajoute son `DbSet` dans `IAppDbContext`.
- **Audit** : les entités tracées héritent de `EntiteAuditable` ; ne jamais renseigner `DateCreation`/`CreePar`/`DateModification`/`ModifiePar` à la main.
- **Services** : valider explicitement avec `IValidator<T>`, retourner `ResultatOperation` / `ResultatOperation<T>` pour les erreurs métier (pas d'exception), `CancellationToken jetonAnnulation` sur chaque méthode asynchrone.
- **Dates** : `TimeProvider` injecté (`GetUtcNow().UtcDateTime`), jamais `DateTime.Now`. Utiliser `DateTime` en base (les tests unitaires s'exécutent sur SQLite, qui ne trie pas les `DateTimeOffset`).
- **Système externe** (fichiers, SMTP, HTTP, Excel) : définir une interface ici, l'implémenter dans Infrastructure.
- **Mapping** : `IMapper` injecté ; jamais `TypeAdapterConfig.GlobalSettings`.
- **DI** : enregistrer les services dans `DependencyInjection.AddCore` (Scoped). Les widgets `IWidgetTableauDeBord` sont découverts automatiquement.
- **Permissions** : ajouter les constantes dans `Autorisation/Permissions.cs`, les inclure dans `Toutes` et dans `RolesApplication.PermissionsParRole`.
