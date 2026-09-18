---
name: preparer-pr
description: Prépare la branche courante d'erpWeb pour une Pull Request (mise à jour depuis main, vérification des commits et migrations, build, format, tests, revue de conception) puis ouvre la PR avec gh ou fournit les commandes à exécuter. À utiliser quand une évolution est terminée.
---

# Préparer une Pull Request

## 1. Contrôles de la branche

1. `git branch --show-current` : refuser de continuer sur `main`.
2. `git status --short` : proposer de commiter les modifications restantes (Conventional Commits).
3. Si un dépôt distant existe : `git fetch origin`, puis mettre la branche à jour avec `main` (`git merge origin/main`). En cas de conflit sur le snapshot EF Core, déléguer à l'agent `expert-migrations`.
4. Commits : `git log --format=%s main..HEAD` ; tous doivent respecter `<type>(<portee>): <description>`. Signaler les écarts.
5. Migrations : `git diff --name-only main...HEAD -- src/erpWeb.Infrastructure/Donnees/Migrations` ; plus d'une migration → proposer de les regrouper via `expert-migrations`.

## 2. Vérifications (identiques à la CI)

```bash
dotnet build erpWeb.sln
dotnet format erpWeb.sln --verify-no-changes
dotnet test erpWeb.sln
```

Toute erreur est corrigée avant de continuer (commit `fix` ou `refactor` dédié).

## 3. Revue

Lancer l'agent `revue-conception`. Présenter les constats à l'utilisateur ; corriger les **bloquants** avant l'ouverture de la PR, proposer les autres.

## 4. Description de la Pull Request

Rédiger le corps à partir de `.github/pull_request_template.md` : description (quoi et pourquoi, à partir des commits et du diff), type de changement, migration, checklist cochée **uniquement** pour les points réellement vérifiés aux étapes précédentes.
Titre : message Conventional Commits résumant la branche (il deviendra le commit de squash merge).

## 5. Publication

Pousser et ouvrir une PR sont des actions visibles par d'autres : **demander confirmation** à l'utilisateur avant de continuer.

- Si `gh --version` réussit et qu'un dépôt distant existe :

  ```bash
  git push -u origin HEAD
  gh pr create --base main --title "<titre>" --body-file <fichier temporaire>
  ```

  Afficher l'URL de la PR et rappeler : fusion en squash merge après CI verte et approbation.

- Sinon : afficher les commandes ci-dessus et le corps de la PR prêt à copier, en indiquant ce qui manque (`gh` non installé : `winget install GitHub.cli`, ou dépôt distant absent : `git remote add origin <url>`).
