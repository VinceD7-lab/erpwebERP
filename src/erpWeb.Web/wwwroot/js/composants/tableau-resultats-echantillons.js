'use strict';

// Îlot Vue : filtres (période de tournée, client, statut) + grille Tabulator paginée côté serveur,
// regroupée par date de tournée puis par client.
// Vue possède les filtres, l'état de chargement et l'alerte ; Tabulator possède la grille.

import { obtenirJson, urlConnexion } from './api.js';
import { optionsGrilleParDefaut } from './grille.js';

// Liste blanche : un champ hors de cette table n'est pas envoyé au serveur.
const champsTri = {
  codeBarresAnonyme: 'CodeBarres',
  filiere: 'Filiere',
  statutAnalyse: 'StatutAnalyse',
};

const apparencesStatut = {
  Recu: { libelle: 'Reçu', couleur: 'secondary' },
  EnCours: { libelle: 'En cours', couleur: 'primary' },
  Termine: { libelle: 'Terminé', couleur: 'success' },
};

const formatDate = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short' });
const formatDateHeure = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'short' });
const formatNombre = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 3 });

/** « 2026-09-15 » (DateOnly) est lu comme une date locale : pas de décalage de fuseau. */
function formaterDateTournee(valeur) {
  return valeur ? formatDate.format(new Date(valeur + 'T00:00')) : 'Sans tournée';
}

function formaterNombre(cellule) {
  const valeur = cellule.getValue();
  return valeur === null || valeur === undefined ? '' : formatNombre.format(valeur);
}

function formaterStatut(cellule) {
  const apparence = apparencesStatut[cellule.getValue()] ?? { libelle: cellule.getValue(), couleur: 'secondary' };
  const badge = document.createElement('span');
  badge.className = 'badge text-bg-' + apparence.couleur;
  badge.textContent = apparence.libelle;
  return badge;
}

function formaterValidation(cellule) {
  const valeur = cellule.getValue();
  return valeur ? formatDateHeure.format(new Date(valeur)) : '';
}

/** Clé de regroupement : une ligne de groupe par couple (date de tournée, client). */
function cleGroupe(echantillon) {
  return formaterDateTournee(echantillon.dateTournee) + ' — ' + (echantillon.raisonSocialeClient ?? 'Sans client');
}

function enteteGroupe(valeur, nombre) {
  const entete = document.createElement('span');
  // textContent : la raison sociale est une donnée saisie par les utilisateurs.
  entete.textContent = valeur + ' (' + nombre + ')';
  return entete;
}

export default {
  props: {
    urlDonnees: { type: String, required: true },
    clients: { type: String, default: '[]' },
    taillePage: { type: String, default: '25' },
  },

  data() {
    return { dateDebut: '', dateFin: '', idClient: '', statut: '', chargement: false, erreur: '', codeErreur: 0 };
  },

  computed: {
    lienConnexion() {
      return urlConnexion();
    },

    listeClients() {
      try {
        return JSON.parse(this.clients);
      } catch {
        return [];
      }
    },
  },

  mounted() {
    // Hors de data() : l'instance Tabulator ne doit pas être rendue réactive.
    this.grille = new Tabulator(this.$refs.grille, {
      ...optionsGrilleParDefaut(),
      ajaxURL: this.urlDonnees,
      paginationSize: Number(this.taillePage),
      // Pas de tri initial : le serveur trie par défaut sur la date de tournée (décroissante) puis le client,
      // ce qui rend contigus les échantillons d'un même groupe.
      groupBy: cleGroupe,
      groupHeader: enteteGroupe,
      columns: [
        { title: 'Code-barres', field: 'codeBarresAnonyme', minWidth: 170 },
        { title: 'Filière', field: 'filiere', width: 140 },
        { title: 'Statut', field: 'statutAnalyse', width: 110, formatter: formaterStatut },
        { title: 'Support', field: 'typeSupport', width: 100, headerSort: false },
        { title: 'Temp. réception (°C)', field: 'temperatureReception', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'pH sol', field: 'phSol', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'Mat. org.', field: 'matiereOrganique', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'P2O5', field: 'phosphoreP2O5', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'K2O', field: 'potassiumK2O', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'Reliquat N', field: 'reliquatAzoteN', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'UCL fourrage', field: 'valeurUclFourrage', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'Validation', field: 'dateValidation', width: 140, headerSort: false, formatter: formaterValidation },
      ],
      // Pas d'ajaxURLGenerator : Tabulator ne l'appelle que dans son chargeur par défaut, que
      // ajaxRequestFunc remplace. L'URL (page, tri, filtres) est donc construite ici.
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

      // Tri par défaut du serveur (date puis client) tant que l'utilisateur n'a pas choisi de colonne.
      const tri = parametres.sorters?.[0];
      if (tri && champsTri[tri.field]) {
        requete.set('tri', champsTri[tri.field]);
        requete.set('triDescendant', String(tri.dir === 'desc'));
      }

      if (this.dateDebut) {
        requete.set('dateTourneeDebut', this.dateDebut);
      }
      if (this.dateFin) {
        requete.set('dateTourneeFin', this.dateFin);
      }
      if (this.idClient) {
        requete.set('idClient', this.idClient);
      }
      if (this.statut) {
        requete.set('statutAnalyse', this.statut);
      }

      return url + '?' + requete.toString();
    },

    appliquerFiltres() {
      // setData() sans argument relance la requête distante sur la page courante : un filtre plus
      // restrictif laisserait une page inexistante, on repart donc de la première.
      if (this.grille.getPage() > 1) {
        this.grille.setPage(1);
      } else {
        this.grille.setData();
      }
    },

    reinitialiser() {
      this.dateDebut = '';
      this.dateFin = '';
      this.idClient = '';
      this.statut = '';
      this.appliquerFiltres();
    },
  },

  template: `
    <form class="row g-2 align-items-end mb-3" role="search" @submit.prevent="appliquerFiltres">
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="dateDebutEchantillons">Tournées du</label>
        <input id="dateDebutEchantillons" class="form-control form-control-sm" type="date" v-model="dateDebut">
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="dateFinEchantillons">au</label>
        <input id="dateFinEchantillons" class="form-control form-control-sm" type="date" v-model="dateFin">
      </div>
      <div class="col-12 col-md-3">
        <label class="form-label small mb-1" for="clientEchantillons">Client</label>
        <select id="clientEchantillons" class="form-select form-select-sm" v-model="idClient">
          <option value="">Tous</option>
          <option v-for="client in listeClients" :key="client.Id" :value="client.Id">{{ client.RaisonSociale }}</option>
        </select>
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="statutEchantillons">Statut</label>
        <select id="statutEchantillons" class="form-select form-select-sm" v-model="statut">
          <option value="">Tous</option>
          <option value="Recu">Reçu</option>
          <option value="EnCours">En cours</option>
          <option value="Termine">Terminé</option>
        </select>
      </div>
      <div class="col-6 col-md-3 d-flex gap-2">
        <button class="btn btn-sm btn-primary" type="submit">Filtrer</button>
        <button class="btn btn-sm btn-outline-secondary" type="button" @click="reinitialiser">Réinitialiser</button>
      </div>
    </form>

    <div v-if="erreur" class="alert alert-warning d-flex justify-content-between align-items-center gap-2" role="alert">
      <span>{{ erreur }}</span>
      <a v-if="codeErreur === 401" class="btn btn-sm btn-outline-secondary" :href="lienConnexion">Se reconnecter</a>
    </div>

    <div ref="grille" :aria-busy="chargement ? 'true' : 'false'"></div>
    <p class="visually-hidden" role="status">{{ chargement ? 'Chargement des résultats' : '' }}</p>
  `,
};
