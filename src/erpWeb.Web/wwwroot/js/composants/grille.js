'use strict';

// Options communes des grilles Tabulator du socle : localisation française et pagination serveur.
// Une nouvelle grille part de optionsGrilleParDefaut() plutôt que de recopier ces réglages.

export const langueGrilles = {
  'fr-fr': {
    data: { loading: 'Chargement…', error: 'Erreur de chargement' },
    pagination: {
      page_size: 'Éléments par page',
      page_title: 'Afficher la page',
      first: '«',
      first_title: 'Première page',
      last: '»',
      last_title: 'Dernière page',
      prev: '‹',
      prev_title: 'Page précédente',
      next: '›',
      next_title: 'Page suivante',
      all: 'Tous',
      counter: { showing: 'Éléments', of: 'sur', rows: 'entrées', pages: 'pages' },
    },
  },
};

export function optionsGrilleParDefaut() {
  return {
    layout: 'fitColumns',
    locale: 'fr-fr',
    langs: langueGrilles,
    pagination: true,
    paginationMode: 'remote',
    sortMode: 'remote',
    paginationCounter: 'rows',
    paginationSizeSelector: [25, 50, 100, 200],
    placeholder: 'Aucune donnée disponible',
  };
}
