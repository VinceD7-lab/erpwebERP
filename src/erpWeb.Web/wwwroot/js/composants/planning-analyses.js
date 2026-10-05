'use strict';

// Îlot Vue : planning mensuel des types d'analyse (lignes = clients, colonnes = jours du mois).
// Le serveur décide des jours ouvrés et des droits ; l'îlot n'en déduit rien.

import { ErreurAppel, envoyerJson, obtenirJson, urlConnexion } from './api.js';

const typesAnalyse = ['A', 'B', 'C', 'D'];

const formatMois = new Intl.DateTimeFormat('fr-FR', { month: 'long', year: 'numeric' });
const formatJourSemaine = new Intl.DateTimeFormat('fr-FR', { weekday: 'short' });

/** « 2026-10-05 » (DateOnly) est lu comme une date locale : pas de décalage de fuseau. */
function lireDate(texte) {
  return new Date(texte + 'T00:00');
}

function cleCase(idClient, date) {
  return idClient + '|' + date;
}

export default {
  props: {
    urlDonnees: { type: String, required: true },
    urlSauvegarde: { type: String, required: true },
    annee: { type: String, default: '' },
    mois: { type: String, default: '' },
    peutGerer: { type: String, default: 'false' },
  },

  data() {
    return {
      planning: null,
      valeurs: {},
      modifie: false,
      chargement: false,
      enregistrement: false,
      erreur: '',
      codeErreur: 0,
      succes: '',
    };
  },

  computed: {
    lienConnexion() {
      return urlConnexion();
    },

    modifiable() {
      return this.peutGerer === 'true';
    },

    titreMois() {
      if (!this.planning) {
        return '';
      }
      const titre = formatMois.format(new Date(this.planning.annee, this.planning.mois - 1, 1));
      return titre.charAt(0).toUpperCase() + titre.slice(1);
    },
  },

  mounted() {
    this.charger(this.annee, this.mois);
  },

  methods: {
    typesAnalyse() {
      return typesAnalyse;
    },

    numeroJour(jour) {
      return lireDate(jour.date).getDate();
    },

    libelleJourSemaine(jour) {
      return formatJourSemaine.format(lireDate(jour.date));
    },

    cle(client, jour) {
      return cleCase(client.id, jour.date);
    },

    async charger(annee, mois) {
      this.chargement = true;
      this.erreur = '';
      this.codeErreur = 0;
      this.succes = '';

      try {
        const parametres = new URLSearchParams();
        if (annee && mois) {
          parametres.set('annee', annee);
          parametres.set('mois', mois);
        }
        const planning = await obtenirJson(this.urlDonnees + '?' + parametres.toString());

        const valeurs = {};
        for (const caseplanning of planning.cases) {
          valeurs[cleCase(caseplanning.idClient, caseplanning.date)] = caseplanning.typeAnalyse;
        }
        this.planning = planning;
        this.valeurs = valeurs;
        this.modifie = false;
      } catch (erreur) {
        this.signaler(erreur);
      } finally {
        this.chargement = false;
      }
    },

    changerMois(decalage) {
      if (this.modifie && !window.confirm('Les modifications non enregistrées seront perdues. Continuer ?')) {
        return;
      }
      const date = new Date(this.planning.annee, this.planning.mois - 1 + decalage, 1);
      this.charger(String(date.getFullYear()), String(date.getMonth() + 1));
    },

    modifierCase(client, jour, valeur) {
      const cle = this.cle(client, jour);
      if (valeur) {
        this.valeurs[cle] = valeur;
      } else {
        delete this.valeurs[cle];
      }
      this.modifie = true;
      this.succes = '';
    },

    async sauvegarder() {
      this.enregistrement = true;
      this.erreur = '';
      this.codeErreur = 0;
      this.succes = '';

      const cases = [];
      for (const client of this.planning.clients) {
        for (const jour of this.planning.jours) {
          const typeAnalyse = this.valeurs[cleCase(client.id, jour.date)];
          if (typeAnalyse) {
            cases.push({ idClient: client.id, date: jour.date, typeAnalyse });
          }
        }
      }

      try {
        await envoyerJson(this.urlSauvegarde, { annee: this.planning.annee, mois: this.planning.mois, cases });
        this.modifie = false;
        this.succes = 'Le planning de ' + this.titreMois.toLowerCase() + ' a été enregistré.';
      } catch (erreur) {
        this.signaler(erreur);
      } finally {
        this.enregistrement = false;
      }
    },

    signaler(erreur) {
      this.codeErreur = erreur instanceof ErreurAppel ? erreur.statut : 0;
      this.erreur = erreur instanceof ErreurAppel ? erreur.message : 'Le planning n\'a pas pu être chargé.';
    },
  },

  template: `
    <div>
      <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
        <button type="button" class="btn btn-outline-secondary" aria-label="Mois précédent"
                :disabled="!planning || chargement" @click="changerMois(-1)">
          <i class="bi bi-chevron-left" aria-hidden="true"></i>
        </button>
        <h2 class="h5 mb-0 flex-grow-1 text-center" aria-live="polite">{{ titreMois }}</h2>
        <button type="button" class="btn btn-outline-secondary" aria-label="Mois suivant"
                :disabled="!planning || chargement" @click="changerMois(1)">
          <i class="bi bi-chevron-right" aria-hidden="true"></i>
        </button>
        <button v-if="modifiable" type="button" class="btn btn-primary ms-2"
                :disabled="!planning || !modifie || enregistrement" @click="sauvegarder">
          <i class="bi bi-save me-1" aria-hidden="true"></i>Sauvegarder
        </button>
      </div>

      <div v-if="erreur" class="alert alert-danger" role="alert">
        {{ erreur }}
        <a v-if="codeErreur === 401" :href="lienConnexion">Se reconnecter</a>
      </div>
      <div v-if="succes" class="alert alert-success" role="status">{{ succes }}</div>
      <p v-if="chargement" class="small text-body-secondary" role="status">Chargement du planning…</p>

      <p v-if="planning && planning.clients.length === 0" class="text-body-secondary">Aucun client à planifier.</p>

      <div v-else-if="planning" class="table-responsive planning-analyses">
        <table class="table table-sm table-bordered align-middle mb-0">
          <thead>
            <tr>
              <th scope="col" class="planning-client">Client</th>
              <th v-for="jour in planning.jours" :key="jour.date" scope="col"
                  class="text-center" :class="{ 'planning-weekend': !jour.estOuvre }">
                <div>{{ numeroJour(jour) }}</div>
                <div class="small fw-normal text-body-secondary">{{ libelleJourSemaine(jour) }}</div>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="client in planning.clients" :key="client.id">
              <th scope="row" class="planning-client fw-normal">{{ client.raisonSociale }}</th>
              <td v-for="jour in planning.jours" :key="jour.date"
                  class="text-center p-1" :class="{ 'planning-weekend': !jour.estOuvre }">
                <select v-if="jour.estOuvre" class="form-select form-select-sm"
                        :aria-label="'Type d\\'analyse de ' + client.raisonSociale + ' le ' + jour.date"
                        :disabled="!modifiable || enregistrement"
                        :value="valeurs[cle(client, jour)] || ''"
                        @change="modifierCase(client, jour, $event.target.value)">
                  <option value=""></option>
                  <option v-for="type in typesAnalyse()" :key="type" :value="type">{{ type }}</option>
                </select>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
};
