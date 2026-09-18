# Contribuer à erpWeb

Le projet suit le **GitHub Flow**. La spécification complète (architecture, principes SOLID/STUPID) est dans [docs/prompt-generation-socle-erp.md](docs/prompt-generation-socle-erp.md).

## Flux de travail

1. `main` est **toujours déployable** : aucun commit direct, toute modification passe par une Pull Request.
2. Créer une branche courte depuis `main` à jour :

   ```bash
   git switch main && git pull
   git switch -c fonctionnalite/gestion-clients
   ```

3. Faire des commits petits et atomiques, pousser régulièrement.
4. Ouvrir une Pull Request dès que possible (en brouillon si le travail est en cours) ; remplir le modèle.
5. Fusionner uniquement si la CI est verte et après au moins une relecture approuvée.
6. Fusion en **squash merge**, puis suppression de la branche.

## Nommage des branches

Format : `<type>/<description-courte-en-kebab-case>`, sans accents.

| Type | Usage | Exemple |
| --- | --- | --- |
| `fonctionnalite/` | Nouvelle fonctionnalité | `fonctionnalite/gestion-clients` |
| `correctif/` | Correction de bug | `correctif/calcul-montant-tva` |
| `refactorisation/` | Refactorisation sans changement fonctionnel | `refactorisation/service-facturation` |
| `documentation/` | Documentation uniquement | `documentation/readme-lancement` |
| `technique/` | CI, dépendances, configuration | `technique/mise-a-jour-ef-core` |

## Messages de commit

[Conventional Commits](https://www.conventionalcommits.org/fr/), description en français :

```text
feat(utilisateurs): ajouter la désactivation d'un compte
fix(documents): refuser les fichiers vides
```

Types autorisés : `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `ci`.

## Migrations EF Core

- Au plus **une migration par Pull Request**, nommée en français :

  ```bash
  dotnet ef migrations add AjoutTableClients -p src/erpWeb.Infrastructure -s src/erpWeb.Web -o Donnees/Migrations
  ```

- Avant fusion, mettre la branche à jour depuis `main`. En cas de conflit sur `AppDbContextModelSnapshot.cs`, supprimer la migration de la branche (`dotnet ef migrations remove`) et la régénérer.
- Ne jamais modifier une migration déjà fusionnée dans `main`.

## Vérifications avant la Pull Request

Les mêmes contrôles sont exécutés par la CI et bloquent la fusion :

```bash
dotnet build erpWeb.sln
dotnet format erpWeb.sln --verify-no-changes
dotnet test erpWeb.sln
```

## Avec Claude Code

Le dépôt fournit des instructions (`CLAUDE.md`), des hooks (protection de `main`, format des commits, règles STUPID, build en fin de tour), des agents et des skills (`/nouveau-module`, `/nouveau-widget`, `/demarrer-branche`, `/preparer-pr`). Voir la section « Outillage Claude Code » du [README](README.md).
