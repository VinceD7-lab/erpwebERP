'use strict';

// Îlot Vue : barre de filtres réactive + grille Tabulator paginée côté serveur.
// Vue possède les filtres, l'état de chargement et l'alerte ; Tabulator possède la grille.

import { obtenirJson, urlConnexion } from './api.js';
import { optionsGrilleParDefaut } from './grille.js';

// Liste blanche : un champ hors de cette table n'est pas envoyé au serveur.
const champsTri = { date: 'Date', utilisateur: 'Utilisateur', typeEntite: 'TypeEntite', action: 'Action' };

const apparencesAction = {
  Creation: { libelle: 'Création', couleur: 'success' },
  Modification: { libelle: 'Modification', couleur: 'primary' },
  Suppression: { libelle: 'Suppression', couleur: 'danger' },
};

const formatHorodatage = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short', timeStyle: 'medium' });

function formaterDate(cellule) {
  const valeur = cellule.getValue();
  return valeur ? formatHorodatage.format(new Date(valeur)) : '';
}

function formaterAction(cellule) {
  const apparence = apparencesAction[cellule.getValue()] ?? { libelle: cellule.getValue(), couleur: 'secondary' };
  const badge = document.createElement('span');
  badge.className = 'badge text-bg-' + apparence.couleur;
  badge.textContent = apparence.libelle;
  return badge;
}

function ajouterBloc(parent, titre, contenu) {
  if (!contenu) {
    return;
  }

  const intitule = document.createElement('div');
  intitule.className = 'small fw-semibold mt-1';
  intitule.textContent = titre;

  const bloc = document.createElement('pre');
  bloc.className = 'small bg-body-tertiary p-2 mb-1 text-wrap text-break';
  // textContent et non innerHTML : le JSON audité contient des données saisies par les utilisateurs.
  bloc.textContent = contenu;

  parent.append(intitule, bloc);
}

function formaterDetails(cellule) {
  const entree = cellule.getRow().getData();
  if (!entree.anciennesValeurs && !entree.nouvellesValeurs) {
    return '';
  }

  const details = document.createElement('details');
  const resume = document.createElement('summary');
  resume.className = 'small';
  resume.textContent = 'Valeurs';
  details.append(resume);

  ajouterBloc(details, 'Avant', entree.anciennesValeurs);
  ajouterBloc(details, 'Après', entree.nouvellesValeurs);

  // L'ouverture change la hauteur de la ligne : Tabulator doit la recalculer.
  details.addEventListener('toggle', () => cellule.getRow().normalizeHeight());
  return details;
}

export default {
  props: {
    urlDonnees: { type: String, required: true },
    taillePage: { type: String, default: '25' },
  },

  data() {
    return { recherche: '', action: '', dateDebut: '', dateFin: '', chargement: false, erreur: '', statut: 0 };
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
      ajaxURL: this.urlDonnees,
      paginationSize: Number(this.taillePage),
      initialSort: [{ column: 'date', dir: 'desc' }],
      columns: [
        { title: 'Date', field: 'date', width: 170, formatter: formaterDate },
        { title: 'Utilisateur', field: 'utilisateur' },
        { title: 'Action', field: 'action', width: 130, formatter: formaterAction },
        { title: 'Entité', field: 'typeEntite', width: 150 },
        { title: 'Identifiant', field: 'idEntite', headerSort: false },
        { title: 'Détails', field: 'nouvellesValeurs', headerSort: false, variableHeight: true, formatter: formaterDetails },
      ],
      ajaxURLGenerator: (url, configuration, parametres) => this.construireUrl(url, parametres),
      ajaxRequestFunc: (url) => obtenirJson(url),
      ajaxResponse: (url, parametres, reponse) => ({
        last_page: reponse.nombrePages,
        last_row: reponse.nombreTotal,
        data: reponse.elements,
      }),
    });

    this.grille.on('dataLoading', () => {
      this.chargement = true;
      this.erreur = '';
      this.statut = 0;
    });
    this.grille.on('dataLoaded', () => {
      this.chargement = false;
    });
    this.grille.on('dataLoadError', (erreur) => {
      this.chargement = false;
      this.statut = erreur.statut ?? 0;
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

      const tri = parametres.sorters?.[0];
      if (tri && champsTri[tri.field]) {
        requete.set('tri', champsTri[tri.field]);
        requete.set('triDescendant', String(tri.dir === 'desc'));
      }

      const recherche = this.recherche.trim();
      if (recherche) {
        requete.set('recherche', recherche);
      }
      if (this.action) {
        requete.set('action', this.action);
      }
      if (this.dateDebut) {
        requete.set('dateDebut', new Date(this.dateDebut + 'T00:00').toISOString());
      }
      if (this.dateFin) {
        // Borne supérieure exclue côté serveur : on envoie le lendemain pour inclure le jour choisi.
        const lendemain = new Date(this.dateFin + 'T00:00');
        lendemain.setDate(lendemain.getDate() + 1);
        requete.set('dateFin', lendemain.toISOString());
      }

      return url + '?' + requete.toString();
    },

    appliquerFiltres() {
      // setData() sans argument relance la requête distante en repartant de la page 1.
      this.grille.setData();
    },

    reinitialiser() {
      this.recherche = '';
      this.action = '';
      this.dateDebut = '';
      this.dateFin = '';
      this.appliquerFiltres();
    },
  },

  template: `
    <form class="row g-2 align-items-end mb-3" role="search" @submit.prevent="appliquerFiltres">
      <div class="col-12 col-md-4">
        <label class="form-label small mb-1" for="rechercheJournal">Rechercher</label>
        <input id="rechercheJournal" class="form-control form-control-sm" type="search"
               v-model="recherche" placeholder="Utilisateur, entité, identifiant">
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="actionJournal">Action</label>
        <select id="actionJournal" class="form-select form-select-sm" v-model="action">
          <option value="">Toutes</option>
          <option value="Creation">Création</option>
          <option value="Modification">Modification</option>
          <option value="Suppression">Suppression</option>
        </select>
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="dateDebutJournal">Du</label>
        <input id="dateDebutJournal" class="form-control form-control-sm" type="date" v-model="dateDebut">
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="dateFinJournal">Au</label>
        <input id="dateFinJournal" class="form-control form-control-sm" type="date" v-model="dateFin">
      </div>
      <div class="col-6 col-md-2 d-flex gap-2">
        <button class="btn btn-sm btn-primary" type="submit">Filtrer</button>
        <button class="btn btn-sm btn-outline-secondary" type="button" @click="reinitialiser">Réinitialiser</button>
      </div>
    </form>

    <div v-if="erreur" class="alert alert-warning d-flex justify-content-between align-items-center gap-2" role="alert">
      <span>{{ erreur }}</span>
      <a v-if="statut === 401" class="btn btn-sm btn-outline-secondary" :href="lienConnexion">Se reconnecter</a>
    </div>

    <div ref="grille" :aria-busy="chargement ? 'true' : 'false'"></div>
    <p class="visually-hidden" role="status">{{ chargement ? 'Chargement des entrées du journal' : '' }}</p>
  `,
};
