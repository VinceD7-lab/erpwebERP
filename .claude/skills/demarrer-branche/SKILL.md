---
name: demarrer-branche
description: Démarre une évolution selon le GitHub Flow d'erpWeb : met main à jour et crée une branche <type>/<description> correctement nommée. À utiliser avant tout développement, ou dès que Claude constate qu'il travaille sur main.
---

# Démarrer une branche (GitHub Flow)

1. **État du dépôt** : `git status --short`.
   - Modifications en cours : demander à l'utilisateur s'il faut les commiter sur la branche actuelle, les mettre de côté (`git stash`) ou les emporter dans la nouvelle branche. Ne jamais les supprimer.
2. **Mettre main à jour** :
   - `git switch main`
   - si un dépôt distant existe (`git remote` non vide) : `git pull --ff-only`
3. **Déterminer le type** d'après la demande :

   | Type | Quand |
   | --- | --- |
   | `fonctionnalite/` | nouvelle fonctionnalité, nouveau module ou widget |
   | `correctif/` | correction de bug |
   | `refactorisation/` | restructuration sans changement fonctionnel |
   | `documentation/` | documentation uniquement |
   | `technique/` | CI, dépendances, configuration, outillage |

4. **Description** : 2 à 5 mots en français, kebab-case, sans accents ni articles superflus (`gestion-clients`, `calcul-montant-tva`). Si la demande est ambiguë, proposer le nom et le faire valider.
5. **Créer la branche** : `git switch -c <type>/<description>`.
6. Confirmer à l'utilisateur la branche créée et rappeler la suite : commits Conventional Commits, puis `/preparer-pr` avant d'ouvrir la Pull Request.
