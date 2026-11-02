'use strict';

// Îlot Vue : barre de recherche + grille Tabulator paginée côté serveur.
// Vue possède la recherche, l'état de chargement et l'alerte ; Tabulator possède la grille.

import { obtenirJson, urlConnexion } from './api.js';
import { langueGrilles, optionsGrilleParDefaut } from './grille.js';

// Liste blanche : un champ hors de cette table n'est pas envoyé au serveur.
const champsTri = {
  numero: 'Numero',
  raisonSocialeClient: 'Client',
  dateDevis: 'DateDevis',
  dateValidite: 'DateValidite',
  statut: 'Statut',
  objet: 'Objet',
  montantTotalToutesTaxesComprises: 'MontantTotalToutesTaxesComprises',
};

const apparencesStatut = {
  Brouillon: { libelle: 'Brouillon', couleur: 'secondary' },
  Envoye: { libelle: 'Envoyé', couleur: 'primary' },
  Accepte: { libelle: 'Accepté', couleur: 'success' },
  Refuse: { libelle: 'Refusé', couleur: 'danger' },
  Expire: { libelle: 'Expiré', couleur: 'warning' },
  Annule: { libelle: 'Annulé', couleur: 'dark' },
};

const formatDate = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short' });
const formatNombre = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

// Barre de navigation sous le tableau : chevrons en HTML statique, les titres restent les noms accessibles.
const langueAvecChevrons = {
  'fr-fr': {
    ...langueGrilles['fr-fr'],
    pagination: {
      ...langueGrilles['fr-fr'].pagination,
      first: '<i class="bi bi-chevron-double-left" aria-hidden="true"></i>',
      prev: '<i class="bi bi-chevron-left" aria-hidden="true"></i>',
      next: '<i class="bi bi-chevron-right" aria-hidden="true"></i>',
      last: '<i class="bi bi-chevron-double-right" aria-hidden="true"></i>',
    },
  },
};

/** « 2026-09-15T00:00:00 » (sans fuseau) est lu comme une date locale : pas de décalage. */
function formaterDate(cellule) {
  const valeur = cellule.getValue();
  return valeur ? formatDate.format(new Date(valeur)) : '';
}

/** Montant dans la devise du devis ; repli sur un nombre simple si le code devise est inconnu d'Intl. */
function formaterMontant(cellule) {
  const valeur = cellule.getValue();
  if (valeur === null || valeur === undefined) {
    return '';
  }
  const devise = cellule.getData().devise;
  try {
    return new Intl.NumberFormat('fr-FR', { style: 'currency', currency: devise }).format(valeur);
  } catch {
    return formatNombre.format(valeur) + ' ' + devise;
  }
}

function formaterStatut(cellule) {
  const apparence = apparencesStatut[cellule.getValue()] ?? { libelle: cellule.getValue(), couleur: 'secondary' };
  const badge = document.createElement('span');
  badge.className = 'badge text-bg-' + apparence.couleur;
  badge.textContent = apparence.libelle;
  return badge;
}

export default {
  props: {
    urlDonnees: { type: String, required: true },
    taillePage: { type: String, default: '10' },
  },

  data() {
    return { recherche: '', chargement: false, erreur: '', codeErreur: 0 };
  },

  computed: {
    lienConnexion() {
      return urlConnexion();
    },
  },

  mounted() {
    // Hors de data() : l'instance Tabulator ne doit pas être rendue réactive.
    this.grille = new Tabulator(this.$refs.grille, {
      ...optionsGrilleParDefaut(),
      langs: langueAvecChevrons,
      ajaxURL: this.urlDonnees,
      paginationSize: Number(this.taillePage),
      paginationSizeSelector: [10, 25, 50, 100],
      columns: [
        { title: 'N°', field: 'numero', width: 140 },
        { title: 'Client', field: 'raisonSocialeClient', minWidth: 170 },
        { title: 'Objet', field: 'objet', minWidth: 170 },
        { title: 'Référence', field: 'reference', width: 120, headerSort: false },
        { title: 'Date', field: 'dateDevis', width: 100, formatter: formaterDate },
        { title: 'Validité', field: 'dateValidite', width: 100, formatter: formaterDate },
        { title: 'Statut', field: 'statut', width: 110, formatter: formaterStatut },
        { title: 'Montant HT', field: 'montantTotalHorsTaxe', width: 120, hozAlign: 'right', headerSort: false, formatter: formaterMontant },
        { title: 'TVA', field: 'montantTotalTaxe', width: 110, hozAlign: 'right', headerSort: false, formatter: formaterMontant },
        { title: 'Montant TTC', field: 'montantTotalToutesTaxesComprises', width: 120, hozAlign: 'right', formatter: formaterMontant },
      ],
      // Pas d'ajaxURLGenerator : Tabulator ne l'appelle que dans son chargeur par défaut, que
      // ajaxRequestFunc remplace. L'URL (page, tri, recherche) est donc construite ici.
      ajaxRequestFunc: (url, configuration, parametres) => obtenirJson(this.construireUrl(url, parametres)),
      ajaxResponse: (url, parametres, reponse) => ({
        last_page: reponse.nombrePages,
        last_row: reponse.nombreTotal,
        data: reponse.elements,
      }),
    });

    this.grille.on('dataLoading', () => {
      this.chargement = true;
      this.erreur = '';
      this.codeErreur = 0;
    });
    this.grille.on('dataLoaded', () => {
      this.chargement = false;
    });
    this.grille.on('dataLoadError', (erreur) => {
      this.chargement = false;
      this.codeErreur = erreur.statut ?? 0;
      this.erreur = erreur.message || 'Chargement impossible.';
    });
  },

  beforeUnmount() {
    this.grille?.destroy();
  },

  methods: {
    construireUrl(url, parametres) {
      const requete = new URLSearchParams();
      requete.set('numeroPage', parametres.page ?? 1);
      requete.set('taillePage', parametres.size ?? this.taillePage);

      // Tri par défaut du serveur (raison sociale croissante) tant que l'utilisateur n'a pas choisi de colonne.
      const tri = parametres.sorters?.[0];
      if (tri && champsTri[tri.field]) {
        requete.set('tri', champsTri[tri.field]);
        requete.set('triDescendant', String(tri.dir === 'desc'));
      }

      const texte = this.recherche.trim();
      if (texte) {
        requete.set('recherche', texte);
      }

      return url + '?' + requete.toString();
    },

    rechercher() {
      // setData() sans argument relance la requête distante sur la page courante : une recherche plus
      // restrictive laisserait une page inexistante, on repart donc de la première.
      if (this.grille.getPage() > 1) {
        this.grille.setPage(1);
      } else {
        this.grille.setData();
      }
    },

    reinitialiser() {
      this.recherche = '';
      this.rechercher();
    },
  },

  template: `
    <form class="row g-2 align-items-end mb-3" role="search" @submit.prevent="rechercher">
      <div class="col-12 col-md-6">
        <label class="form-label small mb-1" for="rechercheDevis">Rechercher un devis</label>
        <input id="rechercheDevis" class="form-control form-control-sm" type="search" maxlength="100"
               placeholder="Numéro, client, objet ou référence" v-model="recherche">
      </div>
      <div class="col-12 col-md-6 d-flex gap-2">
        <button class="btn btn-sm btn-primary" type="submit">
          <i class="bi bi-search me-1" aria-hidden="true"></i>Rechercher
        </button>
        <button class="btn btn-sm btn-outline-secondary" type="button" @click="reinitialiser">Réinitialiser</button>
      </div>
    </form>

    <div v-if="erreur" class="alert alert-warning d-flex justify-content-between align-items-center gap-2" role="alert">
      <span>{{ erreur }}</span>
      <a v-if="codeErreur === 401" class="btn btn-sm btn-outline-secondary" :href="lienConnexion">Se reconnecter</a>
    </div>

    <div ref="grille" :aria-busy="chargement ? 'true' : 'false'"></div>
    <p class="visually-hidden" role="status">{{ chargement ? 'Chargement des devis' : '' }}</p>
  `,
};
