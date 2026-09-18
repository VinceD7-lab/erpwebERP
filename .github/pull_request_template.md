## Description

<!-- Quoi et pourquoi. Lier le ticket le cas échéant. -->

## Type de changement

- [ ] `feat` — nouvelle fonctionnalité
- [ ] `fix` — correction de bug
- [ ] `refactor` — refactorisation sans changement fonctionnel
- [ ] `docs` — documentation
- [ ] `test` — tests uniquement
- [ ] `chore` / `ci` — dépendances, configuration, intégration continue

## Migration EF Core

- [ ] Aucune migration
- [ ] Une migration : `NomDeLaMigration` (branche à jour avec `main`, snapshot sans conflit)

## Checklist

- [ ] Branche nommée `<type>/<description>` et à jour avec `main`
- [ ] Commits au format Conventional Commits
- [ ] Tests ajoutés ou mis à jour ; `dotnet test` vert
- [ ] `dotnet format erpWeb.sln --verify-no-changes` sans écart
- [ ] Principes SOLID respectés (service par cas d'usage, extension par interfaces, dépendances injectées)
- [ ] Aucun anti-pattern STUPID (état statique, `new` de service, `DateTime.Now`, optimisation non mesurée, abréviations, duplication)
- [ ] Contrôleurs sans logique métier ; services dépendant de `IAppDbContext`, jamais de `AppDbContext`
- [ ] Nouvelles actions protégées par une permission (`[Authorize(Policy = Permissions.…)]`)
- [ ] CI verte
