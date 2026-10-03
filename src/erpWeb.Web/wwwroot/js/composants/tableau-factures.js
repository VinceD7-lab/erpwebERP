'use strict';

// Îlot Vue : filtres (période de facture, client, statut) + grille Tabulator paginée côté serveur.
// Vue possède les filtres, l'état de chargement et l'alerte ; Tabulator possède la grille.

import { obtenirJson, urlConnexion } from './api.js';
import { optionsGrilleParDefaut } from './grille.js';

// Liste blanche : un champ hors de cette table n'est pas envoyé au serveur.
const champsTri = {
  numeroFacture: 'NumeroFacture',
  dateFacture: 'DateFacture',
  dateEcheance: 'DateEcheance',
  nomClient: 'Client',
  montantToutesTaxesComprises: 'MontantToutesTaxesComprises',
  statutFacture: 'StatutFacture',
};

const apparencesStatut = {
  Emise: { libelle: 'Émise', couleur: 'primary' },
  Payee: { libelle: 'Payée', couleur: 'success' },
  EnRetard: { libelle: 'En retard', couleur: 'danger' },
};

const formatDate = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'short' });
const formatPourcentage = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** « 2026-09-15 » (DateOnly) est lu comme une date locale : pas de décalage de fuseau. */
function formaterDate(cellule) {
  const valeur = cellule.getValue();
  return valeur ? formatDate.format(new Date(valeur + 'T00:00')) : '';
}

function formaterPourcentage(cellule) {
  const valeur = cellule.getValue();
  return valeur === null || valeur === undefined ? '' : formatPourcentage.format(valeur) + ' %';
}

/** Montant dans la devise de la facture ; repli sur un nombre simple si le code devise est inconnu d'Intl. */
function formaterMontant(cellule) {
  const valeur = cellule.getValue();
  if (valeur === null || valeur === undefined) {
    return '';
  }
  const devise = cellule.getData().devise;
  try {
    return new Intl.NumberFormat('fr-FR', { style: 'currency', currency: devise }).format(valeur);
  } catch {
    return formatPourcentage.format(valeur) + ' ' + devise;
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
    urlImpression: { type: String, required: true },
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
      columns: [
        { title: 'N°', field: 'numeroFacture', width: 90, hozAlign: 'right' },
        { title: 'Date', field: 'dateFacture', width: 100, formatter: formaterDate },
        { title: 'Échéance', field: 'dateEcheance', width: 100, formatter: formaterDate },
        { title: 'Code client', field: 'codeClient', width: 110, hozAlign: 'right', headerSort: false },
        { title: 'Client', field: 'nomClient', minWidth: 170 },
        { title: 'Adresse', field: 'adresseFacturation', minWidth: 160, headerSort: false },
        { title: 'Code postal', field: 'codePostal', width: 110, headerSort: false },
        { title: 'Ville', field: 'ville', width: 130, headerSort: false },
        { title: 'Pays', field: 'pays', width: 100, headerSort: false },
        { title: 'Paiement', field: 'modePaiement', width: 110, headerSort: false },
        { title: 'Montant HT', field: 'montantHorsTaxe', width: 120, hozAlign: 'right', headerSort: false, formatter: formaterMontant },
        { title: 'TVA', field: 'tauxTva', width: 80, hozAlign: 'right', headerSort: false, formatter: formaterPourcentage },
        { title: 'Montant TVA', field: 'montantTva', width: 120, hozAlign: 'right', headerSort: false, formatter: formaterMontant },
        { title: 'Montant TTC', field: 'montantToutesTaxesComprises', width: 120, hozAlign: 'right', formatter: formaterMontant },
        { title: 'Remise', field: 'remise', width: 90, hozAlign: 'right', headerSort: false, formatter: formaterPourcentage },
        { title: 'Devise', field: 'devise', width: 80, headerSort: false },
        { title: 'Statut', field: 'statutFacture', width: 110, formatter: formaterStatut },
        { title: 'Date paiement', field: 'datePaiement', width: 120, headerSort: false, formatter: formaterDate },
        { title: 'Commentaire', field: 'commentaire', minWidth: 160, headerSort: false },
        {
          title: 'Impression',
          field: 'idFacture',
          width: 120,
          hozAlign: 'center',
          headerSort: false,
          frozen: true,
          formatter: (cellule) => this.creerBoutonImpression(cellule.getData()),
        },
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
    /** Lien (et non bouton script) : la page imprimable s'ouvre dans un nouvel onglet, sans état à conserver. */
    creerBoutonImpression(facture) {
      const lien = document.createElement('a');
      lien.className = 'btn btn-sm btn-outline-secondary';
      lien.href = this.urlImpression + '/' + encodeURIComponent(facture.idFacture);
      lien.target = '_blank';
      lien.rel = 'noopener';
      lien.setAttribute('aria-label', 'Imprimer la facture ' + facture.numeroFacture);

      const icone = document.createElement('i');
      icone.className = 'bi bi-printer me-1';
      icone.setAttribute('aria-hidden', 'true');
      lien.append(icone, 'Imprimer');
      return lien;
    },

    construireUrl(url, parametres) {
      const requete = new URLSearchParams();
      requete.set('numeroPage', parametres.page ?? 1);
      requete.set('taillePage', parametres.size ?? this.taillePage);

      // Tri par défaut du serveur (date de facture décroissante) tant que l'utilisateur n'a pas choisi de colonne.
      const tri = parametres.sorters?.[0];
      if (tri && champsTri[tri.field]) {
        requete.set('tri', champsTri[tri.field]);
        requete.set('triDescendant', String(tri.dir === 'desc'));
      }

      if (this.dateDebut) {
        requete.set('dateFactureDebut', this.dateDebut);
      }
      if (this.dateFin) {
        requete.set('dateFactureFin', this.dateFin);
      }
      if (this.idClient) {
        requete.set('idClient', this.idClient);
      }
      if (this.statut) {
        requete.set('statutFacture', this.statut);
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
        <label class="form-label small mb-1" for="dateDebutFactures">Factures du</label>
        <input id="dateDebutFactures" class="form-control form-control-sm" type="date" v-model="dateDebut">
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="dateFinFactures">au</label>
        <input id="dateFinFactures" class="form-control form-control-sm" type="date" v-model="dateFin">
      </div>
      <div class="col-12 col-md-3">
        <label class="form-label small mb-1" for="clientFactures">Client</label>
        <select id="clientFactures" class="form-select form-select-sm" v-model="idClient">
          <option value="">Tous</option>
          <option v-for="client in listeClients" :key="client.Id" :value="client.Id">{{ client.RaisonSociale }}</option>
        </select>
      </div>
      <div class="col-6 col-md-2">
        <label class="form-label small mb-1" for="statutFactures">Statut</label>
        <select id="statutFactures" class="form-select form-select-sm" v-model="statut">
          <option value="">Tous</option>
          <option value="Emise">Émise</option>
          <option value="Payee">Payée</option>
          <option value="EnRetard">En retard</option>
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
    <p class="visually-hidden" role="status">{{ chargement ? 'Chargement des factures' : '' }}</p>
  `,
};
