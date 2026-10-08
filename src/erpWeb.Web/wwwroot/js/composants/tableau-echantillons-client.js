'use strict';

// Îlot Vue : grille Tabulator des échantillons du client sélectionné dans la liste des clients.
// La sélection est annoncée par site.js via l'événement « client-selectionne » (detail.idClient) ;
// aucune requête n'est faite tant qu'aucun client n'est sélectionné.

import { obtenirJson, urlConnexion } from './api.js';
import { formaterDateTournee, formaterNombre, formaterStatut, formaterValidation } from './formateurs-echantillons.js';
import { optionsGrilleParDefaut } from './grille.js';

// Liste blanche : un champ hors de cette table n'est pas envoyé au serveur.
const champsTri = {
  codeBarresAnonyme: 'CodeBarres',
  filiere: 'Filiere',
  statutAnalyse: 'StatutAnalyse',
};

export default {
  props: {
    urlDonnees: { type: String, required: true },
    taillePage: { type: String, default: '10' },
  },

  data() {
    return { chargement: false, erreur: '', codeErreur: 0 };
  },

  computed: {
    lienConnexion() {
      return urlConnexion();
    },
  },

  mounted() {
    // Hors de data() : l'instance Tabulator ne doit pas être rendue réactive.
    this.idClient = null;
    this.grille = new Tabulator(this.$refs.grille, {
      ...optionsGrilleParDefaut(),
      paginationSize: Number(this.taillePage),
      paginationSizeSelector: [10, 25, 50],
      columns: [
        { title: 'Date de tournée', field: 'dateTournee', width: 140, headerSort: false, formatter: (cellule) => formaterDateTournee(cellule.getValue()) },
        { title: 'Code-barres', field: 'codeBarresAnonyme', minWidth: 170 },
        { title: 'Filière', field: 'filiere', width: 140 },
        { title: 'Statut', field: 'statutAnalyse', width: 110, formatter: formaterStatut },
        { title: 'Support', field: 'typeSupport', width: 100, headerSort: false },
        { title: 'Temp. réception (°C)', field: 'temperatureReception', hozAlign: 'right', headerSort: false, formatter: formaterNombre },
        { title: 'Validation', field: 'dateValidation', width: 140, headerSort: false, formatter: formaterValidation },
      ],
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

    this.ecouteur = (evenement) => this.afficherClient(evenement.detail.idClient);
    document.addEventListener('client-selectionne', this.ecouteur);
  },

  beforeUnmount() {
    document.removeEventListener('client-selectionne', this.ecouteur);
    this.grille?.destroy();
  },

  methods: {
    construireUrl(url, parametres) {
      const requete = new URLSearchParams();
      requete.set('idClient', this.idClient);
      requete.set('numeroPage', parametres.page ?? 1);
      requete.set('taillePage', parametres.size ?? this.taillePage);

      const tri = parametres.sorters?.[0];
      if (tri && champsTri[tri.field]) {
        requete.set('tri', champsTri[tri.field]);
        requete.set('triDescendant', String(tri.dir === 'desc'));
      }

      return url + '?' + requete.toString();
    },

    /** Charge les échantillons du client ; la grille repart de la première page. */
    afficherClient(idClient) {
      this.idClient = idClient;
      this.grille.setData(this.urlDonnees);
      // La grille a pu être créée dans une zone masquée : on recalcule ses dimensions.
      this.grille.redraw(true);
    },
  },

  template: `
    <div v-if="erreur" class="alert alert-warning d-flex justify-content-between align-items-center gap-2" role="alert">
      <span>{{ erreur }}</span>
      <a v-if="codeErreur === 401" class="btn btn-sm btn-outline-secondary" :href="lienConnexion">Se reconnecter</a>
    </div>

    <div ref="grille" :aria-busy="chargement ? 'true' : 'false'"></div>
    <p class="visually-hidden" role="status">{{ chargement ? 'Chargement des échantillons' : '' }}</p>
  `,
};
