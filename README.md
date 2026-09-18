# erpWeb

Socle applicatif d'ERP pour PME, orienté tableau de bord — ASP.NET Core MVC (.NET 10), EF Core + SQL Server LocalDB.

## Fonctionnalités du socle

- **Tableau de bord** : widgets modulaires (utilisateurs actifs, activité sur 30 jours, derniers documents, raccourcis), filtrés selon les permissions et rafraîchissables à la demande
- **Authentification** : connexion, inscription, déconnexion, verrouillage après échecs, comptes désactivables
- **Autorisation** : rôles (`Admin`, `Utilisateur`) et permissions fines par module et par action
- **Utilisateurs** : liste, création, modification, activation/désactivation, rôles, réinitialisation du mot de passe
- **Journal d'audit** : traçabilité automatique des créations, modifications et suppressions (valeurs avant/après), export Excel
- **Documents** : dépôt, téléchargement et suppression de fichiers rattachés à n'importe quelle entité
- **Paramètres** : paramètres clé/valeur en base, surchargeant la configuration sans redéploiement

## Prérequis

- [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- Outil EF Core : `dotnet tool install --global dotnet-ef` (ou `dotnet tool update --global dotnet-ef`)
- SQL Server LocalDB (instance `MSSQLLocalDB`) : installé avec Visual Studio (charge de travail « Stockage et traitement des données ») ou via l'installateur SQL Server Express (option LocalDB) ; vérifier avec `sqllocaldb info MSSQLLocalDB`
- Accès réseau au premier build : les bibliothèques frontend (Bootstrap, Chart.js, DataTables…) sont restaurées par LibMan

## Démarrage

```bash
# 1. Mot de passe du compte administrateur (stocké hors du dépôt)
dotnet user-secrets set "Administrateur:MotDePasse" "<MotDePasse#Solide1>" --project src/erpWeb.Web

# 2. Création de la base erpWeb sur LocalDB
dotnet ef database update -p src/erpWeb.Infrastructure -s src/erpWeb.Web

# 3. Lancement
dotnet run --project src/erpWeb.Web
```

Ouvrir <http://localhost:5271> et se connecter avec `admin@erpweb.local` et le mot de passe défini à l'étape 1.

> En environnement `Development`, les migrations sont aussi appliquées automatiquement au démarrage
> (`BaseDeDonnees:AppliquerMigrationsAuDemarrage`). L'administrateur, les rôles et leurs permissions
> sont créés à chaque démarrage s'ils n'existent pas.

## Configuration

| Clé | Rôle | Valeur par défaut |
| --- | --- | --- |
| `ConnectionStrings:ParDefaut` | Base SQL Server | `Server=(localdb)\MSSQLLocalDB;Database=erpWeb;…` |
| `Administrateur:Email` / `NomComplet` | Compte administrateur initial | `admin@erpweb.local` |
| `Administrateur:MotDePasse` | Mot de passe initial (**secrets utilisateur uniquement**) | — |
| `BaseDeDonnees:AppliquerMigrationsAuDemarrage` | Migration automatique au démarrage | `true` en Development |
| `Stockage:Dossier` / `TailleMaximaleOctets` | Dossier des documents, taille maximale | `App_Data/documents`, 10 Mo |
| `Smtp:Hote`, `Port`, `NomUtilisateur`, `MotDePasse`… | Envoi d'emails (MailKit) ; hôte vide = emails journalisés | hôte vide |

Les paramètres saisis dans l'écran **Paramètres** (clé au format `Section:Cle`, ex. `Smtp:Hote`) sont lus en dernier et surchargent ces valeurs.

## Architecture

```text
erpWeb.sln
├── src/
│   ├── erpWeb.Core/             # Entités, interfaces, services, DTOs, validateurs, widgets (par module)
│   ├── erpWeb.Infrastructure/   # AppDbContext, configurations et migrations EF Core, audit, email, stockage, export
│   └── erpWeb.Web/              # Program.cs, contrôleurs, vues Razor, Tag Helpers, wwwroot
├── tests/
│   ├── erpWeb.UnitTests/        # Services, intercepteur, stockage, widgets, règles d'architecture
│   └── erpWeb.IntegrationTests/ # Parcours HTTP complets (WebApplicationFactory)
└── docs/prompt-generation-socle-erp.md
```

- `Core` ne dépend d'aucun autre projet ; `Infrastructure` → `Core` ; `Web` → `Core` + `Infrastructure` (uniquement dans `Program.cs`)
- Pas de repositories : les services utilisent `IAppDbContext`, implémentée par `AppDbContext`
- Nommage en français ; versions NuGet centralisées dans `Directory.Packages.props`

### Rôles et permissions

| Permission | Admin | Utilisateur |
| --- | :---: | :---: |
| `Utilisateurs.Lire`, `Utilisateurs.Gerer` | ✓ | |
| `JournalAudit.Lire`, `JournalAudit.Exporter` | ✓ | |
| `Documents.Lire`, `Documents.Deposer` | ✓ | ✓ |
| `Documents.Supprimer` | ✓ | |
| `Parametres.Lire`, `Parametres.Modifier` | ✓ | |

Les permissions sont définies dans `src/erpWeb.Core/Autorisation` et synchronisées en base au démarrage.

## Principes de conception

Le code applique les principes **SOLID** et proscrit les anti-patterns **STUPID** ; les règles vérifiables le sont automatiquement.

| Règle | Application | Vérification |
| --- | --- | --- |
| Single Responsibility | Un service par module ; contrôleurs minces | Relecture (agent `revue-conception`) |
| Open/Closed | Widgets `IWidgetTableauDeBord` découverts automatiquement ; audit par intercepteur | Test `AddCore_EnregistreAutomatiquementTousLesWidgets` |
| Liskov / Interface Segregation | Interfaces petites (`IStockageFichiers`, `IServiceEmail`, `ILectureJournalAudit`) | Relecture |
| Dependency Inversion | Interfaces dans Core, implémentations dans Infrastructure | Tests d'architecture NetArchTest |
| Pas de Singleton / Tight coupling | Durées de vie gérées par la DI, aucune instanciation de service | Hook `verifier-regles`, tests d'architecture |
| Testabilité | `TimeProvider`, SQLite en mémoire pour les tests unitaires, aucune dépendance statique | 50+ tests |
| Nommage explicite | Conventions `.editorconfig` | `dotnet format --verify-no-changes` (CI) |
| Pas de duplication | `EntiteAuditable`, partials, Tag Helpers, configurations EF | Relecture |

## Tests

```bash
dotnet test erpWeb.sln                                            # tous les tests
dotnet test tests/erpWeb.UnitTests --filter "FullyQualifiedName~Architecture"   # règles d'architecture
```

Les tests unitaires utilisent SQLite en mémoire. Les tests d'intégration créent puis suppriment une base `erpWeb_Tests_<guid>` sur LocalDB, avec les migrations réelles ; pour un autre serveur (CI, conteneur), définir la variable `ERPWEB_TESTS_SQLSERVER` (chaîne de connexion sans nom de base).

## Gestion de version — GitHub Flow

Branches `<type>/<description>`, commits Conventional Commits, une Pull Request par évolution, squash merge.
Détails : [CONTRIBUTING.md](CONTRIBUTING.md). La CI (`.github/workflows/ci.yml`) exécute build, vérification du format et tests sur chaque Pull Request.

### Protection de la branche main

À configurer sur GitHub (**Settings → Branches → Add rule** pour `main`, et **Settings → General → Pull Requests**) :

- Pull Request obligatoire avant fusion, **1 approbation** minimum, approbations invalidées par un nouveau commit
- Contrôle de statut `build-test` requis, branche à jour avant fusion
- Historique linéaire, force push et suppression interdits
- **Squash merge** uniquement, suppression automatique des branches fusionnées

Équivalent avec GitHub CLI :

```bash
gh api -X PUT repos/<proprietaire>/<depot>/branches/main/protection \
  -H "Accept: application/vnd.github+json" \
  --input - <<'JSON'
{
  "required_status_checks": { "strict": true, "contexts": ["build-test"] },
  "enforce_admins": true,
  "required_pull_request_reviews": { "required_approving_review_count": 1, "dismiss_stale_reviews": true },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false
}
JSON

gh api -X PATCH repos/<proprietaire>/<depot> \
  -F allow_squash_merge=true -F allow_merge_commit=false -F allow_rebase_merge=false -F delete_branch_on_merge=true
```

## Outillage Claude Code

| Élément | Emplacement | Rôle |
| --- | --- | --- |
| Instructions | `CLAUDE.md` (racine, `src/*/`, `tests/`) | Commandes, architecture, règles de conception et de nommage |
| Permissions | `.claude/settings.json` | Commandes autorisées ; `git push --force` et `dotnet ef database drop` interdits |
| Hooks Git | `.claude/hooks/garde-git.ps1`, `valider-commit.ps1`, `garde-migrations.ps1` | Aucun commit sur `main`, Conventional Commits, migrations fusionnées protégées |
| Hooks qualité | `.claude/hooks/verifier-regles.ps1`, `formater.ps1`, `verifier-build.ps1` | Anti-patterns STUPID signalés, formatage, build et tests d'architecture en fin de tour |
| Agents | `.claude/agents/` | `revue-conception`, `redacteur-tests`, `expert-migrations` |
| Skills | `.claude/skills/` | `/nouveau-module`, `/nouveau-widget`, `/demarrer-branche`, `/preparer-pr` |

Les hooks nécessitent Windows PowerShell (`powershell.exe`).
