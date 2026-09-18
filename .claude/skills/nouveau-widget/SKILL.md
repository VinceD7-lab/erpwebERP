---
name: nouveau-widget
description: Ajoute un widget au tableau de bord d'erpWeb (implémentation d'IWidgetTableauDeBord, rendu générique par type de données, permission, tests). À utiliser quand l'utilisateur demande un indicateur, un graphique, une liste ou des raccourcis sur le tableau de bord.
---

# Nouveau widget du tableau de bord

Les widgets sont découverts automatiquement (`DependencyInjection.AddCore`) : **aucune modification** du contrôleur ni de la page n'est nécessaire (Open/Closed).

## 1. Cadrage

Obtenir si non fournis : titre, donnée affichée et sa source (service Core existant ou `IAppDbContext`), permission requise (ou aucune), position (`Ordre` : les widgets existants utilisent 10, 20, 30, 40).

Choisir le type de données existant le plus adapté :

| Type | Rendu | Exemple |
| --- | --- | --- |
| `DonneesIndicateur(Valeur, Libelle, Detail)` | Chiffre clé | `WidgetUtilisateursActifs` |
| `DonneesGraphique(TypeGraphique, Etiquettes, Series)` | Graphique Chart.js (`line`, `bar`, `doughnut`…) | `WidgetActiviteAudit` |
| `DonneesListe(Elements, MessageVide)` | Liste de liens | `WidgetDerniersDocuments` |
| `DonneesRaccourcis(Raccourcis)` | Boutons filtrés par permission | `WidgetRaccourcisAdministration` |

## 2. Implémentation

1. Créer `src/erpWeb.Core/TableauDeBord/Widgets/Widget<Nom>.cs` : `sealed`, implémente `IWidgetTableauDeBord`.
   - `Nom` : kebab-case unique (utilisé dans `/TableauDeBord/Widget/{nom}`).
   - `Icone` : classe Bootstrap Icons (`bi-…`).
   - `PermissionRequise` : constante de `Permissions`, ou `null`.
   - Dépendances : services Core ou `IAppDbContext`, jamais Infrastructure ; dates via `TimeProvider`.
   - Déléguer les requêtes non triviales à un service Core plutôt que de les écrire dans le widget.
2. **Seulement si aucun type existant ne convient** :
   - ajouter un `sealed record Donnees<Type>(...) : DonneesWidget` dans `TableauDeBord/DonneesWidget.cs` ;
   - créer la vue `src/erpWeb.Web/Views/Shared/Components/Widget/Donnees<Type>.cshtml` (nom identique au type) ;
   - si du JavaScript est requis, l'ajouter dans `wwwroot/js/site.js` avec une initialisation appelable après rafraîchissement.

## 3. Tests

- `tests/erpWeb.UnitTests/Core/WidgetsTests.cs` : test du widget (données sources simulées, type et contenu du résultat) ; **mettre à jour le nombre attendu** dans `AddCore_EnregistreAutomatiquementTousLesWidgets`.
- `tests/erpWeb.IntegrationTests/ParcoursApplicationTests.cs` : ajouter le `Nom` du widget à la liste de `TableauDeBord_Administrateur_AfficheLesQuatreWidgets` (renommer le test si le nombre change).

## 4. Vérifications

`dotnet build erpWeb.sln`, `dotnet test erpWeb.sln`, puis commit `feat(tableau-de-bord): widget <titre>`.
