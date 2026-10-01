# MAJ Base de données

Notes :

- Base cible : SQL Server (LocalDB en développement). Les tests unitaires restent sur SQLite en mémoire, ce qui impose `DateTime` / `DateOnly` (pas de `DateTimeOffset`) côté entités.
- Les types .NET sont indiqués entre parenthèses. « Taille » = longueur max (`HasMaxLength`), `—` si non définie.
- `PK` = clé primaire (`int IDENTITY`), `FK` = clé étrangère, `UQ` = index unique.
- Toutes les entités héritent de `EntiteAuditable` : la clé primaire s'appelle `Id` et les colonnes d'audit (`DateCreation`, `CreePar`, `DateModification`, `ModifiePar`) sont renseignées par l'intercepteur, jamais à la main.
- Les chaînes sont en `nvarchar(n)` (Unicode), les `DateTime` en `datetime2`, les `DateOnly` en `date`.

## Vue d'ensemble des relations

| Table parent | Table enfant | Colonne FK | Suppression |
|---|---|---|---|
| TourneesRamassage | Echantillons | IdTournee (nullable) | Restrict |
| Clients | Echantillons | IdClient (nullable) | Restrict |
| Echantillons | ResultatsAgronomie | IdEchantillon (1-1) | Cascade |

---

## TourneesRamassage

| Champ | Type SQL Server (type .NET) | Taille | Null | Clé / Index |
|---|---|---|---|---|
| Id | int IDENTITY (int) | — | Non | PK |
| DateTournee | date (DateOnly) | — | Non | Index `IX_TourneesRamassage_DateTournee` |
| NomChauffeur | nvarchar (string) | 50 | Non | |
| ImmatriculationCamion | nvarchar (string) | 15 | Oui | |
| StatutTemperature | nvarchar (string) | 20 | Oui | |
| DateCreation, CreePar, DateModification, ModifiePar | datetime2, nvarchar, datetime2, nvarchar | — | voir `EntiteAuditable` | Audit |

## Echantillons

| Champ | Type SQL Server (type .NET) | Taille | Null | Clé / Index |
|---|---|---|---|---|
| Id | int IDENTITY (int) | — | Non | PK |
| CodeBarresAnonyme | nvarchar (string) | 50 | Non | UQ `IX_Echantillons_CodeBarresAnonyme` |
| DatePrelevement | datetime2 (DateTime) | — | Non | |
| Filiere | nvarchar (string) | 30 | Non | |
| StatutAnalyse | nvarchar (string) | 20 | Non | |
| TemperatureReception | decimal(5,2) (decimal?) | — | Oui | CHECK `TemperatureReception BETWEEN -10 AND 100` |
| IdTournee | int (int?) | — | Oui | FK → TourneesRamassage (Restrict), index |
| IdClient | int (int?) | — | Oui | FK → Clients (Restrict), index |
| DateCreation, CreePar, DateModification, ModifiePar | datetime2, nvarchar, datetime2, nvarchar | — | voir `EntiteAuditable` | Audit |

## ResultatsAgronomie

| Champ | Type SQL Server (type .NET) | Taille | Null | Clé / Index |
|---|---|---|---|---|
| Id | int IDENTITY (int) | — | Non | PK |
| IdEchantillon | int (int) | — | Non | FK → Echantillons (Cascade), UQ |
| TypeSupport | nvarchar (string) | 50 | Oui | |
| PhSol | decimal(4,2) (decimal?) | — | Oui | |
| MatiereOrganique | decimal(6,3) (decimal?) | — | Oui | |
| PhosphoreP2O5 | decimal(8,3) (decimal?) | — | Oui | |
| PotassiumK2O | decimal(8,3) (decimal?) | — | Oui | |
| ReliquatAzoteN | decimal(8,3) (decimal?) | — | Oui | |
| ValeurUclFourrage | decimal(6,3) (decimal?) | — | Oui | |
| DateValidation | datetime2 (DateTime?) | — | Oui | |
| DateCreation, CreePar, DateModification, ModifiePar | datetime2, nvarchar, datetime2, nvarchar | — | voir `EntiteAuditable` | Audit |
