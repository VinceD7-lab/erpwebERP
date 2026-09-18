# Tests

```bash
dotnet test erpWeb.sln
dotnet test tests/erpWeb.UnitTests --filter "FullyQualifiedName~Architecture"
```

## Conventions

- Nommage `Methode_Scenario_ResultatAttendu` ; blocs Arrange / Act / Assert séparés par une ligne vide.
- Au moins un test par service et par widget ; couvrir les cas d'échec (validation, élément introuvable).

## Tests unitaires (`erpWeb.UnitTests`)

- `Outils/BaseDonneesTest` : SQLite en mémoire + `IntercepteurAudit` + `FakeTimeProvider` (`DateReference`) + utilisateur courant simulé ; `BaseDonneesTest.CreerMapper()` pour Mapster. Ne pas mocker les `DbSet`.
- Moq pour les interfaces externes (`IStockageFichiers`, `IServiceEmail`, `IRechargementConfiguration`).
- Bogus `new Faker("fr")` pour des données réalistes ; emails générés sur `@erpweb.local`.
- Services basés sur Identity : utiliser l'implémentation réelle via `ServiceCollection` (modèle : `Core/ServiceUtilisateursTests.cs`), pas de mock de `UserManager`.
- `Architecture/` : ajouter une règle NetArchTest dès qu'une nouvelle contrainte de dépendance apparaît.
- Dans `namespace erpWeb.UnitTests.Core`, qualifier `global::erpWeb.Core` en cas d'ambiguïté.

## Tests d'intégration (`erpWeb.IntegrationTests`)

- `IClassFixture<FabriqueApplication>` : environnement `Test`, base SQL Server temporaire `erpWeb_Tests_<guid>` supprimée à la fin (LocalDB, ou serveur défini par la variable `ERPWEB_TESTS_SQLSERVER`), administrateur `FabriqueApplication.EmailAdministrateur`.
- Client sans redirection automatique : `CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`.
- `ExtensionsClient.ConnecterAsync` et `EnvoyerFormulaireAsync` gèrent le jeton antiforgery.
