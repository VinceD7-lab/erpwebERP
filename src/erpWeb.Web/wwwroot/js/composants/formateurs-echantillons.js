'use strict';

// Formateurs de cellules Tabulator communs aux grilles d'échantillons.

const apparencesStatut = {
  Recu: { libelle: 'Reçu', couleur: 'secondary' },
  EnCours: { libelle: 'En cours', couleur: 'primary' },
  Termine: { libelle: 'Terminé', couleur: 'success' },
};

const formatDate = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short' });
const formatDateHeure = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' });
const formatNombre = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 3 });

/** « 2026-09-15 » (DateOnly) est lu comme une date locale : pas de décalage de fuseau. */
export function formaterDateTournee(valeur) {
  return valeur ? formatDate.format(new Date(valeur + 'T00:00')) : 'Sans tournée';
}

export function formaterNombre(cellule) {
  const valeur = cellule.getValue();
  return valeur === null || valeur === undefined ? '' : formatNombre.format(valeur);
}

export function formaterStatut(cellule) {
  const apparence = apparencesStatut[cellule.getValue()] ?? { libelle: cellule.getValue(), couleur: 'secondary' };
  const badge = document.createElement('span');
  badge.className = 'badge text-bg-' + apparence.couleur;
  badge.textContent = apparence.libelle;
  return badge;
}

export function formaterValidation(cellule) {
  const valeur = cellule.getValue();
  return valeur ? formatDateHeure.format(new Date(valeur)) : '';
}
