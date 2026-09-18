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

- Bibliothèques déclarées dans `libman.json` (restaurées au build, `wwwroot/lib` non versionné).
- JavaScript dans `wwwroot/js/site.js` (pas de script inline), styles dans `wwwroot/css/site.css`.
