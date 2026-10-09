# erpWeb.Web — présentation MVC

`Program.cs` est la seule référence à Infrastructure (composition root).

## Contrôleurs

- `sealed`, minces : appeler un service Core, puis vue ou redirection (Post/Redirect/Get).
- `[Authorize(Policy = Permissions.<Module>.Lire)]` sur la classe, la permission d'écriture sur chaque action de modification.
- Erreurs métier : `ModelState.AjouterErreurs(resultat.Erreurs)` ; succès : `TempData[CleMessages.Succes]`.
- Actions sans suffixe `Async` (le nom est l'URL) ; `CancellationToken jetonAnnulation` en dernier paramètre.
- Jamais d'`IAppDbContext` ni d'EF Core dans un contrôleur (test d'architecture).
- Antiforgery appliqué globalement : tout formulaire POST utilise les Tag Helpers (`asp-action`).

## Vues

- En-tête : `<en-tete-page titre="…" icone="bi-…">boutons</en-tete-page>`.
- Masquer selon les droits : attribut `asp-permission="@Permissions.X.Y"` (liens, boutons, entrées de menu).
- Listes : `<table class="table …" data-tableau>` (DataTables), `data-order` pour trier dates et tailles, `data-orderable="false"` sur la colonne Actions.
- Actions destructrices : `<form … data-confirmation="Message ?">`.
- Accessibilité : `aria-label` sur les boutons icônes, `scope="col"`, icônes en `aria-hidden="true"`.
- Formulaires : lier directement les DTOs Core ; créer un ViewModel dans `Modeles/` seulement pour la présentation (`IFormFile`, connexion).
- Menu : `Views/Shared/_Layout.cshtml`. Widgets : une vue par type de données dans `Views/Shared/Components/Widget/`.

## Frontend

- Bibliothèques déclarées dans `libman.json` (restaurées au build, `wwwroot/lib` non versionné). Aucun Node, aucun bundler : les bibliothèques sont des builds globaux, les composants des modules ES natifs.
- Police Inter Variable hébergée localement : restaurée par `libman.json` (`wwwroot/lib/inter/`), déclarée en `@font-face` dans `site.css` et appliquée via `--bs-font-sans-serif`. Pas de CDN de polices.
- JavaScript transverse dans `wwwroot/js/site.js` (pas de script inline), styles dans `wwwroot/css/site.css`.

### Îlots Vue

Vue 3 sert des zones interactives ciblées ; Razor reste le rendu serveur et le routage.

- Point de montage dans la vue : `<div data-composant-vue="nom-du-composant" data-…="…">repli</div>`, plus `<partial name="_ScriptsVue" />` dans `@section Scripts`.
- **Open/Closed** : `nom-du-composant` correspond au module `wwwroot/js/composants/nom-du-composant.js`, qui exporte par défaut ses options Vue. Ajouter un composant = ajouter un fichier ; `montage.js` n'est jamais modifié.
- Les `data-*` deviennent les propriétés du composant et sont **toujours des chaînes** (un booléen se lit `=== 'true'`, un nombre passe par `Number(...)`).
- Appels JSON : toujours via `composants/api.js` (`obtenirJson`), qui pose les en-têtes et traduit 401/403 en messages. Grilles : partir de `composants/grille.js` (`optionsGrilleParDefaut`).
- CSS spécifique à une page : `@section Styles`.

**Règle bloquante — aucune syntaxe de template Vue dans une vue Razor.** Dans un `.cshtml`, `@` ouvre une expression Razor : `@click`, `@submit` et `{{ … }}` précédé de `@` sont interprétés côté serveur. L'échappement `@@click` fonctionne mais rend les vues illisibles. Le `.cshtml` ne contient que le point de montage et ses `data-*` ; le template (`v-if`, `@click`, `v-model`, `{{ … }}`) vit exclusivement dans la propriété `template` du module `.js`. Les délimiteurs par défaut de Vue ne sont pas modifiés.

**Règle bloquante — `textContent`, jamais de HTML concaténé.** Tout affichage de données issues de la base (valeurs auditées, saisies utilisateur) se construit avec `document.createElement` + `textContent`. Concaténer du HTML dans un formateur de grille est une faille XSS stockée.

- Permissions : elles restent évaluées côté serveur (`asp-permission`). Un îlot ne décide jamais d'un droit ; ce qui doit être masqué l'est dans le `.cshtml`.
- Nommage du JavaScript comme celui du C# : français, sans accents ni abréviations.
- DataTables (`data-tableau`) reste en place sur les listes non encore migrées ; les deux mécanismes cohabitent sans conflit.
