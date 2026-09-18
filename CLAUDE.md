# erpWeb — instructions pour Claude Code

Socle ERP PME en ASP.NET Core MVC (.NET 10), EF Core + SQL Server LocalDB (tests unitaires sur SQLite en mémoire). Spécification complète : [docs/prompt-generation-socle-erp.md](docs/prompt-generation-socle-erp.md).

## Commandes

```bash
dotnet build erpWeb.sln
dotnet test erpWeb.sln
dotnet format erpWeb.sln --verify-no-changes
dotnet ef migrations add <NomEnFrancais> -p src/erpWeb.Infrastructure -s src/erpWeb.Web
dotnet ef database update -p src/erpWeb.Infrastructure -s src/erpWeb.Web
dotnet run --project src/erpWeb.Web
```

## Architecture

- `src/erpWeb.Core` : entités, interfaces, services, DTOs, validateurs, widgets. Aucune dépendance vers les autres projets ; seul package d'accès aux données autorisé : `Microsoft.EntityFrameworkCore`
- `src/erpWeb.Infrastructure` → Core : `AppDbContext` (implémente `IAppDbContext`), configurations EF, migrations, email, stockage
- `src/erpWeb.Web` → Core + Infrastructure (Infrastructure uniquement dans `Program.cs`) : contrôleurs, vues, wwwroot
- Pas de repositories : les services utilisent `IAppDbContext`, jamais `AppDbContext`

## Règles de conception (bloquantes)

- **SRP** : un service par cas d'usage ; contrôleurs minces (requête → service → vue)
- **OCP** : extension par interfaces enregistrées en DI (`IWidgetTableauDeBord`, handlers d'autorisation, intercepteurs)
- **LSP** : aucune `NotImplementedException` dans une implémentation
- **ISP** : interfaces petites et ciblées ; une classe d'options par section
- **DIP** : interfaces dans Core, implémentations dans Infrastructure, injection par constructeur
- Interdits : état statique, singleton maison, Service Locator (`IServiceProvider`), `new` de services, `DateTime.Now` (utiliser `TimeProvider`), optimisation sans mesure, abréviations, copier-coller entre modules

## Conventions

- Nommage **en français**, sans accents ni abréviations (`ServiceUtilisateurs`, `DateCreation`) ; termes du framework conservés (`Controller`, `DbContext`, suffixe `Async`)
- Nullable activé, avertissements traités comme erreurs, `async`/`await` + `CancellationToken` sur les I/O

## GitHub Flow

- Jamais de commit sur `main` : travailler sur `<type>/<description>` (`fonctionnalite/`, `correctif/`, `refactorisation/`, `documentation/`, `technique/`)
- Commits Conventional Commits en français : `feat(module): description`
- Une migration par PR maximum ; ne jamais modifier une migration déjà fusionnée dans `main`

## Outillage

- Instructions détaillées par couche : `src/erpWeb.Core/CLAUDE.md`, `src/erpWeb.Infrastructure/CLAUDE.md`, `src/erpWeb.Web/CLAUDE.md`, `tests/CLAUDE.md`
- Skills : `/demarrer-branche` avant toute évolution, `/nouveau-module`, `/nouveau-widget`, `/preparer-pr` avant d'ouvrir une Pull Request
- Agents : `revue-conception` (relecture SOLID/STUPID, lecture seule), `redacteur-tests`, `expert-migrations`
- Hooks : un retour de `verifier-regles` ou `verifier-build` est bloquant, corriger la cause plutôt que contourner la règle
- Consulter la documentation Microsoft Learn (MCP) avant d'utiliser une API .NET 10 / EF Core 10 récente
