# Prompt Claude Code — Génération du socle applicatif ERP PME (ASP.NET Core MVC)

## Rôle et objectif

Tu es chargé de générer le socle technique d'une application ERP pour PME, en ASP.NET Core MVC (C#). Le socle doit être fonctionnel, buildable dès la génération, et respecter l'architecture, les choix technologiques et les principes de conception décrits ci-dessous.

**Règle générale** : sauf indication contraire explicite, utilise systématiquement la **solution par défaut** indiquée pour chaque dépendance ou module. Les **alternatives** mentionnées sont fournies à titre indicatif (contexte, avantages/inconvénients) et ne doivent être implémentées que sur demande explicite.

**Versionnement de la génération** : initialise le dépôt sur `main` avec un premier commit contenant uniquement `.gitignore`, `.gitattributes` et `README.md`. Génère ensuite le socle sur la branche `fonctionnalite/socle-initial`, en commits atomiques par étape (solution et projets, accès aux données, authentification, modules transverses, tests, CI), en respectant les règles de la section « Gestion de version — GitHub Flow ». Ne fusionne pas dans `main` : la branche est destinée à une Pull Request.

## Stack technique de base

| Élément                 | Choix                                                                     |
| ----------------------- | ------------------------------------------------------------------------- |
| Framework               | **.NET 10 (LTS)**                                                         |
| Pattern d'architecture  | ASP.NET Core MVC + architecture en couches (Core / Infrastructure / Web)  |
| Vues                    | Razor Views                                                               |
| Base de données         | Entity Framework Core + SQL Server LocalDB                                |
| Frontend                | Bootstrap 5.3 + Bootstrap Icons 1.11.3                                    |

## Dépendances techniques (packages NuGet)

### Accès aux données / ORM

- **Défaut** : `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Tools` — ORM standard Microsoft, migrations Code-First, intégration native avec Identity
  - Base locale : SQL Server LocalDB, `Server=(localdb)\MSSQLLocalDB;Database=erpWeb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
- **Pas de repositories** : les services métier accèdent aux données via l'interface `IAppDbContext` (définie dans Core, implémentée par `AppDbContext` dans Infrastructure)
  - `IAppDbContext` expose uniquement les `DbSet<T>` nécessaires et `SaveChangesAsync(CancellationToken)` ; elle n'expose ni `Database`, ni `ChangeTracker`
  - Core référence uniquement le package `Microsoft.EntityFrameworkCore` (pour `DbSet<T>` et LINQ asynchrone), jamais le provider SQL Server

### Identité (comptes, rôles, claims)

- **Défaut** : `Microsoft.AspNetCore.Identity.EntityFrameworkCore` — gestion native des comptes, rôles et claims ; intégration directe avec EF Core

### Mapping objets / DTO

- **Défaut** : `Mapster` + `Mapster.DependencyInjection` — licence MIT, rapide à l'exécution, syntaxe concise ; configuration par module (une classe `IRegister` par module)
- *Alternative écartée* : `AutoMapper` — la dernière version MIT (14.0.0) présente une vulnérabilité de gravité élevée (CVE-2026-32933, déni de service) et les versions corrigées (15.1.1+) sont sous licence commerciale

### Validation

- **Défaut** : `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` — règles de validation complexes et testables, découplées des modèles
  - Le package `FluentValidation.AspNetCore` (validation automatique) est déprécié : appeler explicitement les validateurs dans la couche Core

### Logging

- **Défaut** : `Microsoft.Extensions.Logging` (natif) — suffisant en développement, moins outillé en production
- *Alternative* : `Serilog.AspNetCore` — logs structurés, nombreux sinks (fichiers, Seq, etc.)

### Tâches planifiées / traitements asynchrones

- **Défaut** : `BackgroundService` (natif .NET) — suffisant pour des tâches simples, sans persistance ni supervision
- *Alternative* : `Hangfire` ou `Quartz.NET` — persistance des jobs, planification CRON, tableau de bord de supervision

### Cache

- **Défaut** : `Microsoft.Extensions.Caching.Memory` (natif) — suffisant pour une instance unique
  - À n'utiliser que sur des lectures fréquentes dont le coût a été constaté (pas de cache généralisé)
- *Alternative* : cache distribué (`Microsoft.Extensions.Caching.StackExchangeRedis`) — requis en cas de déploiement multi-instances

### Envoi d'emails

- **Défaut** : `MailKit` — envoi SMTP fiable, remplace `System.Net.Mail` (obsolète/limité)
  - Utilisé uniquement via l'interface `IServiceEmail` (définie dans Core, implémentée dans Infrastructure)

### Génération de documents PDF

- **Défaut** : `QuestPDF` — moderne, API fluide en C#, licence Community gratuite pour les structures dont le CA annuel est inférieur à 1 M$

### Export / import Excel

- **Défaut** : `ClosedXML` — API simple, licence MIT, sans limite d'usage

### Documentation API (si API exposée en complément du MVC)

- **Défaut** : `Swashbuckle.AspNetCore` — génère la documentation Swagger/OpenAPI automatiquement depuis les contrôleurs
- *Alternative* : `Microsoft.AspNetCore.OpenApi` (natif depuis .NET 9) — génération du document OpenAPI sans UI intégrée

### Tests

- `xUnit` + `Moq` — standard pour les tests unitaires .NET
- `Microsoft.EntityFrameworkCore.Sqlite` en mémoire (`DataSource=:memory:`) — tests unitaires des services s'appuyant sur `IAppDbContext` (préférable au mock des `DbSet<T>`)
- SQL Server pour les tests d'intégration — base temporaire par exécution avec les migrations réelles : LocalDB en local, conteneur `mcr.microsoft.com/mssql/server` en CI (variable `ERPWEB_TESTS_SQLSERVER`)
- `Bogus` — génération de données de test réalistes (jeux de démo)
- `Microsoft.AspNetCore.Mvc.Testing` — tests d'intégration des contrôleurs MVC
- `NetArchTest.Rules` — tests d'architecture vérifiant automatiquement les dépendances entre couches

## Bibliothèques frontend (hors NuGet)

À intégrer dans `wwwroot/lib` via **LibMan** (ou CDN) :

- `Bootstrap 5.3` + `Bootstrap Icons 1.11.3` — mise en page responsive, écosystème large
- `DataTables.net` (intégration Bootstrap 5) — grilles de données triables, filtrables et paginées côté client
- `Chart.js` — graphiques du tableau de bord

## Modules transverses du socle

### Authentification & autorisation

- **Défaut** : ASP.NET Core Identity + autorisation par rôles **et** par policies (permissions fines par module/action)
- Une policy par permission, implémentée par un `IAuthorizationRequirement` et un handler dédié

### Gestion des utilisateurs

- **Défaut** : écrans d'administration (réservés au rôle `Admin`) basés sur `UserManager` / `RoleManager` : liste, création, modification, activation/désactivation des comptes, affectation des rôles, réinitialisation du mot de passe

### Tableau de bord

- **Défaut** : page Razor avec widgets modulaires (Partial Views ou View Components) alimentés par Chart.js, rafraîchissement à la demande
- Chaque widget implémente l'interface `IWidgetTableauDeBord` et est découvert via l'injection de dépendances (ajout d'un widget sans modifier la page)

### Journal d'audit

- **Défaut** : interception du `SaveChanges` d'EF Core (`IntercepteurAudit` héritant de `SaveChangesInterceptor`) pour tracer les créations, modifications et suppressions
- Table `JournalAudit` : entité, identifiant, action, utilisateur, date, valeurs avant/après (JSON)

### Gestion documentaire

- **Défaut** : stockage des fichiers sur disque local (dossier dédié, chemin référencé en base) via l'interface `IStockageFichiers` (implémentation `StockageFichiersLocal`), remplaçable par un stockage cloud sans modification du code appelant
- Table `Documents` liée de manière polymorphe aux entités (colonnes `TypeEntite` + `IdEntite`)

### Paramétrage général

- **Défaut** : table `Parametres` clé/valeur en base + pattern `IOptions<T>` pour la configuration typée en code
  - Une classe d'options par section (`OptionsSmtp`, `OptionsStockage`), jamais de lecture statique de la configuration
- *Alternative* : configuration uniquement via `appsettings.json` — plus simple, mais nécessite un redéploiement pour changer un paramètre

## Structure de projet attendue

```text
erpWeb.sln
├── src/
│   ├── erpWeb.Core/             # Entités, règles métier, interfaces (dont IAppDbContext), services applicatifs, DTOs, validateurs
│   ├── erpWeb.Infrastructure/   # AppDbContext (implémente IAppDbContext), configurations et migrations EF Core, envoi d'emails, stockage fichiers
│   └── erpWeb.Web/              # Contrôleurs MVC, Views Razor, wwwroot, configuration DI
└── tests/
    ├── erpWeb.UnitTests/
    └── erpWeb.IntegrationTests/
```

Dépendances entre projets :

- `Core` → aucune dépendance vers les autres projets (seul package d'accès aux données autorisé : `Microsoft.EntityFrameworkCore`)
- `Infrastructure` → `Core`
- `Web` → `Core` + `Infrastructure` (uniquement pour l'enregistrement DI dans `Program.cs`)

## Principes de conception

### Principes SOLID (à appliquer)

| Principe | Application attendue dans le socle |
| --- | --- |
| **S** — Single Responsibility | Un service par cas d'usage ou agrégat (`ServiceFacturation`, pas `ServiceErp`). Contrôleurs limités à : recevoir la requête, appeler un service, retourner une vue. Validation (validateurs FluentValidation), mapping (une configuration Mapster `IRegister` par module) et accès aux données (`IAppDbContext`) séparés. |
| **O** — Open/Closed | Extension sans modification : widgets du tableau de bord via `IWidgetTableauDeBord`, permissions via `IAuthorizationHandler`, audit via `SaveChangesInterceptor`, stockage via `IStockageFichiers`. L'ajout d'un module ne doit pas modifier le code existant du socle. |
| **L** — Liskov Substitution | Toute implémentation d'une interface respecte son contrat, sans `NotImplementedException` ni comportement restreint. Les implémentations de test doivent être substituables sans modifier le code appelant. |
| **I** — Interface Segregation | Interfaces petites et ciblées (`IStockageFichiers`, `IServiceEmail`, `ILectureJournalAudit`). `IAppDbContext` limitée aux `DbSet<T>` et à `SaveChangesAsync`. Options typées découpées par section (`OptionsSmtp`, `OptionsStockage`), pas de classe de configuration globale. |
| **D** — Dependency Inversion | Les interfaces sont définies dans `Core` et implémentées dans `Infrastructure`. Les services dépendent de `IAppDbContext`, jamais de `AppDbContext`. `Core` ne référence ni le provider SQL Server, ni MailKit, et n'accède pas directement au système de fichiers (`File`, `Directory`). Toutes les dépendances sont injectées par constructeur. |

### Anti-patterns STUPID (à proscrire)

| Anti-pattern | Règle |
| --- | --- |
| **S** — Singleton | Aucune classe statique contenant un état, aucun singleton « maison » (`Instance`). Durées de vie gérées par le conteneur DI : `Scoped` pour `AppDbContext` et les services métier, `Singleton` uniquement pour les composants sans état. Pas de Service Locator (injection de `IServiceProvider` interdite hors composition root). |
| **T** — Tight Coupling | Aucune instanciation directe (`new`) de services ou d'infrastructure dans le code métier. `Web` ne référence `Infrastructure` que dans `Program.cs` pour l'enregistrement DI. |
| **U** — Untestability | Aucun accès direct à `DateTime.Now` (utiliser `TimeProvider`), au système de fichiers, au réseau ou à `HttpContext` depuis `Core`. Chaque service métier doit être testable unitairement (mocks et SQLite en mémoire pour `IAppDbContext`). |
| **P** — Premature Optimization | Pas de repositories, CQRS, MediatR, microservices, cache généralisé ni SQL brut sans besoin mesuré. Privilégier un code simple et lisible ; toute optimisation doit être justifiée. |
| **I** — Indescriptive Naming | Noms explicites **en français**, sans accents ni abréviations (`ServiceClient`, `montantTotal`, `DateEcheance` ; pas `SrvCli`, `mnt`). Exceptions : types et termes imposés par le framework (`AppDbContext`, `IAppDbContext`, `Controller`, `ViewModel`, suffixe `Async`). Conventions .NET : PascalCase pour types et membres publics, `_camelCase` pour les champs privés, préfixe `I` pour les interfaces. |
| **D** — Duplication | Logique commune factorisée : classe de base `EntiteAuditable` (Id, dates de création/modification, auteur), Partial Views et Tag Helpers pour les éléments UI répétés, configuration EF Core via `IEntityTypeConfiguration<T>`. Pas de copier-coller entre modules. |

### Contraintes de code

- C# avec nullable reference types activés et avertissements traités comme erreurs (`TreatWarningsAsErrors`)
- `async`/`await` systématique sur les opérations I/O, avec propagation du `CancellationToken`
- Aucune logique métier dans les contrôleurs (délégation vers les services de la couche Core)
- Injection de dépendances par constructeur, via les interfaces définies dans Core

## Gestion de version — GitHub Flow

### Règles du flux

1. `main` est **toujours déployable** : aucun commit direct, toute modification passe par une Pull Request
2. Chaque évolution part d'une branche courte créée depuis `main` à jour
3. Commits petits et atomiques ; la branche est poussée régulièrement
4. Ouverture d'une Pull Request dès que possible (en brouillon si le travail est en cours)
5. Fusion dans `main` uniquement si la CI est verte et après au moins une relecture approuvée
6. Fusion en **squash merge**, puis suppression de la branche
7. Déploiement depuis `main` après fusion (le job de déploiement est hors du périmètre du socle)

### Nommage des branches

Format : `<type>/<description-courte-en-kebab-case>`, sans accents

| Type | Usage | Exemple |
| --- | --- | --- |
| `fonctionnalite/` | Nouvelle fonctionnalité | `fonctionnalite/gestion-utilisateurs` |
| `correctif/` | Correction de bug | `correctif/calcul-montant-tva` |
| `refactorisation/` | Refactorisation sans changement fonctionnel | `refactorisation/service-facturation` |
| `documentation/` | Documentation uniquement | `documentation/readme-lancement` |
| `technique/` | CI, dépendances, configuration | `technique/mise-a-jour-ef-core` |

### Messages de commit

- Convention [Conventional Commits](https://www.conventionalcommits.org/fr/), description en français : `feat(utilisateurs): ajouter la désactivation d'un compte`
- Types autorisés : `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `ci`

### Migrations EF Core

- Au plus une migration par Pull Request, nommée en français (`AjoutTableDocuments`)
- Avant fusion, mettre la branche à jour depuis `main` ; en cas de conflit sur `AppDbContextModelSnapshot`, supprimer la migration de la branche et la régénérer
- Aucune modification d'une migration déjà fusionnée dans `main`

### Contrôles automatisés

| Règle du prompt | Contrôle bloquant avant fusion |
| --- | --- |
| STUPID — Untestability | La CI exécute les tests unitaires, d'intégration et d'architecture |
| Contraintes de code — `TreatWarningsAsErrors` | Build CI en échec en cas d'avertissement |
| STUPID — Indescriptive Naming | Conventions `.editorconfig` vérifiées par `dotnet format --verify-no-changes` |
| Dépendances entre couches | Tests d'architecture NetArchTest |

## Livrables attendus de la génération

1. Solution .NET 10 buildable, structurée en 3 projets (Core, Infrastructure, Web) + 2 projets de tests
2. Migration EF Core initiale incluant les tables du socle (tables Identity `AspNetUsers`/`AspNetRoles`…, `JournalAudit`, `Documents`, `Parametres`)
3. Authentification fonctionnelle (inscription/connexion) avec au moins un utilisateur admin seedé
4. Tableau de bord vide mais fonctionnel (layout + zone widgets)
5. `README.md` avec les instructions de lancement (prérequis, commandes `dotnet ef database update`, `dotnet run`) et une section « Principes de conception » résumant les règles SOLID/STUPID appliquées
6. `appsettings.Development.json` avec la chaîne de connexion locale (SQL Server LocalDB)
7. Tests d'architecture (NetArchTest) vérifiant que `Core` ne dépend ni d'`Infrastructure`, ni de `Web`, ni des providers SQL Server et SQLite, et que les services ne dépendent pas de `AppDbContext`
8. Au moins un test unitaire par service du socle, démontrant sa testabilité
9. Dépôt Git initialisé avec `.gitignore` (`dotnet new gitignore`) et `.gitattributes` (fins de ligne normalisées)
10. Workflow GitHub Actions `.github/workflows/ci.yml`, déclenché sur `pull_request` et `push` vers `main` : `dotnet restore` → `dotnet build` (avertissements = erreurs) → `dotnet format --verify-no-changes` → `dotnet test` (tests unitaires, d'intégration et d'architecture)
11. Modèle de Pull Request `.github/pull_request_template.md` : description, type de changement, migration incluse (oui/non), checklist (tests ajoutés, principes SOLID respectés, aucun anti-pattern STUPID, CI verte)
12. `.editorconfig` imposant les conventions de nommage et de style (vérifiées par `dotnet format` en CI)
13. `CONTRIBUTING.md` décrivant le GitHub Flow, le nommage des branches et les conventions de commit
14. Section « Protection de la branche main » dans le `README.md` : paramètres à activer sur GitHub (PR obligatoire, 1 approbation, CI requise, squash merge uniquement, suppression automatique des branches), avec la commande `gh api` équivalente
15. Outillage Claude Code :
    - `CLAUDE.md` à la racine et dans chaque projet (`src/erpWeb.Core/`, `src/erpWeb.Infrastructure/`, `src/erpWeb.Web/`, `tests/`)
    - `.claude/settings.json` : permissions et hooks (protection de `main`, Conventional Commits, migrations fusionnées, détection des anti-patterns STUPID, formatage, build et tests d'architecture en fin de tour)
    - Agents `.claude/agents/` : `revue-conception` (lecture seule), `redacteur-tests`, `expert-migrations`
    - Skills `.claude/skills/` : `nouveau-module`, `nouveau-widget`, `demarrer-branche`, `preparer-pr`
