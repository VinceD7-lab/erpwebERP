# Branche `fonctionnalite/factures-echantillons`

## Plan : page « Factures » des échantillons + impression

### Contexte

Chaque échantillon doit avoir une facture. Aucune notion de facture ni de tarif n'existe encore. Choix validés : factures **calculées à la volée puis persistées** (table `Factures`, 1-1 avec `Echantillon`), impression via **page HTML imprimable** (`@media print` + `window.print()`, aucune dépendance ajoutée).

Modèle à suivre : module Échantillons (lecture seule, grille Tabulator paginée serveur, îlot Vue) — pas de CRUD, donc pas de `/nouveau-module` complet.

### Core (`src/erpWeb.Core/Factures/`)

- `Facture : EntiteAuditable` — `IdEchantillon` (FK unique, `Echantillon`), `NumeroFacture` (int, unique), `DateFacture` (DateOnly), `DateEcheance?`, `IdClient`, snapshot client figé : `NomClient`(100), `AdresseFacturation?`(255), `CodePostal?`(10), `Ville?`(80), `Pays?`(50), `ModePaiement?`(30), `MontantHorsTaxe`, `TauxTva`(5,2), `MontantTva`, `MontantToutesTaxesComprises` (12,2), `Remise?`(5,2), `Devise`(char 3), `StatutFacture`(20), `DatePaiement?`, `Commentaire?` (max). Noms français sans abréviation, mappés sur les colonnes demandées (CodeClient = `IdClient`).
- `StatutsFacture` (`Emise`, `Payee`, `EnRetard`), `LongueursFacture`.
- `OptionsFacturation` (section `Facturation`) : tarif HT par filière, `TauxTva` (20), `DelaiEcheanceEnJours` (30), `Devise` (EUR), `ModePaiementParDefaut`, remise client optionnelle.
- **Algorithme `CalculateurFacture`** (pur, testable, `TimeProvider`) : montant HT brut = tarif(filière) ; remise % → HT net = brut × (1 − remise/100) ; TVA = HT net × taux ; TTC = HT net + TVA ; arrondis `MidpointRounding.AwayFromZero` à 2 décimales, TTC = HT + TVA exact. Date facture = date de validation du résultat (sinon date de prélèvement/jour courant) ; échéance = date + délai ; statut `EnRetard` si échéance dépassée et non payée ; adresse/nom copiés du `Client`. Échantillon sans client : exclu (listé comme « non facturable »).
- `IGenerationFactures.GenererManquantesAsync(ct)` / `ServiceGenerationFactures` : crée en base les factures absentes (idempotent, `NumeroFacture` = max+1 séquentiel, transaction), appelé à l'ouverture de la liste.
- `IRechercheFactures.RechercherAsync(CriteresFactures, ct)` → `ResultatPagine<FactureDto>` (filtres : période, client, statut ; tri liste blanche), calqué sur `ServiceRechercheResultatsEchantillons`.
- `IServiceImpressionFacture.ObtenirAsync(id, ct)` → `FactureDto` détaillé (+ code-barres/filière de l'échantillon).
- Enregistrement manuel dans `DependencyInjection.cs`; `OptionsFacturation` liée dans `Program.cs`.
- `Permissions.Factures.Lire` (+ ajout à `Toutes` et rôle `Utilisateur`).

### Infrastructure

- `IAppDbContext`/`AppDbContext` : `DbSet<Facture>`.
- `Donnees/Configurations/ConfigurationFacture.cs` (`ConfigurerChampsAudit()`, index uniques sur `NumeroFacture` et `IdEchantillon`, FK `Restrict` vers Client, relation 1-1 Échantillon, précisions decimal, CHECK montants ≥ 0).
- Migration `AjoutFactures` (via agent `expert-migrations`, une seule migration).
- `ServiceClients.SupprimerAsync` : refuser aussi si le client a des factures (déjà couvert par échantillons, vérifier).

### Web

- `FacturesController` (`[Authorize(Policy = Permissions.Factures.Lire)]`, sealed, mince) : `Index` (déclenche génération manquantes puis vue), `Resultats` (JSON paginé), `Imprimer(int id)` (vue facture).
- `Views/Factures/Index.cshtml` : `en-tete-page`, îlot `data-composant-vue="tableau-factures"`, styles/scripts Tabulator comme `Echantillons/Index`.
- `wwwroot/js/composants/tableau-factures.js` : filtres + grille (tous les champs demandés, montants formatés fr-FR, colonne **Imprimer** avec bouton `bi-printer` ouvrant `Factures/Imprimer/{id}` dans un nouvel onglet ; cellules en `textContent`).
- `Views/Factures/Imprimer.cshtml` : mise en page facture (émetteur, client, lignes, HT/remise/TVA/TTC, échéance, mode de paiement, commentaire), bouton « Imprimer » masqué à l'impression, `window.print()` automatique optionnel ; règles `@media print` ajoutées à `wwwroot/css/site.css` (masque sidebar/navbar).
- Menu : entrée dans `_Layout.cshtml` (`asp-permission`, icône `bi-receipt`).

### Données de démonstration

Pas de générateur séparé nécessaire : la génération à la volée crée les factures des échantillons fictifs existants (statut payé/date paiement variés via valeurs déterministes en dev si besoin).

### Tests (agent `redacteur-tests`)

- Unitaires : `CalculateurFactureTests` (arrondis, remise, TVA, TTC, échéance, statut en retard), `ServiceGenerationFacturesTests` (idempotence, numérotation, échantillon sans client), `ServiceRechercheFacturesTests` (filtres, tri, pagination).
- Intégration : `FacturesTests` (Index/Resultats/Imprimer, 401/403, tri inconnu → 400, unicité, FK Restrict).
- Test d'architecture existant doit rester vert (Core sans dépendance).

### Vérification

`dotnet build erpWeb.sln` ; `dotnet test erpWeb.sln` ; `dotnet format erpWeb.sln --verify-no-changes` ; `dotnet run --project src/erpWeb.Web` en Développement : ouvrir Factures, vérifier une ligne par échantillon facturable, cliquer Imprimer et contrôler l'aperçu d'impression ; puis `/preparer-pr` et agent `revue-conception`.

### Point à confirmer à l'implémentation

Les tarifs par filière sont des valeurs de configuration provisoires (`appsettings.json`, section `Facturation`) à remplacer par les vrais tarifs.
