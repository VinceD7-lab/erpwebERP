---
name: nouveau-module
description: Crée un module métier complet dans erpWeb (entité, service, validateurs, mapping, permissions, contrôleur, vues, menu, tests, migration) en suivant l'architecture Core / Infrastructure / Web. À utiliser quand l'utilisateur demande un nouveau module, une nouvelle entité gérée ou un nouvel écran CRUD (ex. clients, fournisseurs, articles).
---

# Nouveau module métier

Module de référence à imiter : **Documents** (`src/erpWeb.Core/Documents`, `DocumentsController`, `Views/Documents`).

## 0. Cadrage

1. Vérifier la branche (`git branch --show-current`) : si `main`, appliquer d'abord le skill `demarrer-branche` (`fonctionnalite/<module>`).
2. Obtenir, si non fournis : nom du module (pluriel, français, ex. `Clients`), entité (singulier, ex. `Client`), champs avec types, obligatoires et longueurs, actions nécessaires (lecture, création, modification, suppression), rôles concernés.
3. Proposer ce découpage à l'utilisateur et attendre sa validation avant de générer.

## 1. Core — `src/erpWeb.Core/<Module>/`

- `<Entite>.cs` : hérite de `EntiteAuditable`, propriétés `string` initialisées à `string.Empty`.
- `Dtos.cs` : `<Entite>Dto` (propriétés `init`), `Creation<Entite>Dto` et `Modification<Entite>Dto` (propriétés `set`, liables par MVC).
- `Validateurs.cs` : un `AbstractValidator<T>` par DTO d'écriture, `WithName` en français, longueurs identiques à la configuration EF.
- `IService<Module>.cs` / `Service<Module>.cs` : `IAppDbContext`, validateurs, `IMapper` ; retours `ResultatOperation` / `ResultatOperation<int>` ; `CancellationToken jetonAnnulation`.
- `ConfigurationMapping<Module>.cs` : `IRegister`.
- `Communs/IAppDbContext.cs` : ajouter `DbSet<<Entite>> <Module> { get; }`.
- `Autorisation/Permissions.cs` : classe imbriquée `<Module>` (`Lire`, `Gerer`…), ajout dans `Toutes` ; `RolesApplication.PermissionsParRole` si le rôle `Utilisateur` est concerné.
- `DependencyInjection.AddCore` : `services.AddScoped<IService<Module>, Service<Module>>();`

## 2. Infrastructure — `src/erpWeb.Infrastructure/`

- `Donnees/Configurations/Configuration<Entite>.cs` : `ToTable("<Module>")`, `ConfigurerChampsAudit()`, `HasMaxLength`, `IsRequired`, index.
- `Donnees/AppDbContext.cs` : `public DbSet<<Entite>> <Module> => Set<<Entite>>();`

## 3. Web — `src/erpWeb.Web/`

- `Controllers/<Module>Controller.cs` : `sealed`, `[Authorize(Policy = Permissions.<Module>.Lire)]`, actions `Index`, `Creer` (GET/POST), `Modifier` (GET/POST), `Supprimer` (POST) protégées par la permission d'écriture ; PRG et `TempData[CleMessages.Succes]`.
- `Views/<Module>/Index.cshtml` : `<en-tete-page>`, bouton protégé par `asp-permission`, tableau `data-tableau`, formulaire de suppression `data-confirmation`.
- `Views/<Module>/Creer.cshtml`, `Modifier.cshtml` : `asp-validation-summary="ModelOnly"`, Tag Helpers `asp-for` ; factoriser les champs communs dans une partial `_Champs<Entite>.cshtml`.
- `Views/Shared/_Layout.cshtml` : entrée de menu `<li class="nav-item" asp-permission="@Permissions.<Module>.Lire">` avec icône Bootstrap Icons.

## 4. Tests

- `tests/erpWeb.UnitTests/Core/Service<Module>Tests.cs` : `BaseDonneesTest`, cas nominal, validation en échec, introuvable, audit renseigné.
- `tests/erpWeb.IntegrationTests/ParcoursApplicationTests.cs` : l'administrateur accède à `/<Module>` (200), un compte inscrit sans permission est redirigé vers `AccesRefuse`.
- Déléguer à l'agent `redacteur-tests` si le volume est important.

## 5. Migration et vérifications

1. Migration via l'agent `expert-migrations` : `Ajout<Module>`.
2. `dotnet build erpWeb.sln`, `dotnet format erpWeb.sln --verify-no-changes`, `dotnet test erpWeb.sln`.
3. Lancer l'agent `revue-conception` et corriger les constats bloquants.
4. Commits Conventional Commits, par exemple `feat(<module>): entité, service et validateurs`, `feat(<module>): écrans et permissions`, `feat(infrastructure): migration Ajout<Module>`, `test(<module>): tests du service et du parcours`.
