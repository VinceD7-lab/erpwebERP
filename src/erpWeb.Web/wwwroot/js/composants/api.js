'use strict';

// Appels JSON authentifiés : en-têtes standards, détection de la session expirée
// (le cookie répond 401 sur les requêtes JSON) et des réponses non JSON.

const enTetesJson = { 'Accept': 'application/json', 'X-Requested-With': 'fetch' };

export class ErreurAppel extends Error {
  constructor(message, statut) {
    super(message);
    this.name = 'ErreurAppel';
    this.statut = statut;
  }
}

/** Préfixe un chemin applicatif par la racine publiée par le gabarit (data-racine sur body). */
export function urlApplication(chemin) {
  return document.body.dataset.racine + chemin.replace(/^\//, '');
}

export function urlConnexion() {
  return urlApplication('Compte/Connexion?ReturnUrl='
    + encodeURIComponent(location.pathname + location.search));
}

/** Message d'une session expirée (401) ou d'un droit manquant (403) ; null pour les autres statuts. */
function erreurAuthentification(reponse, action) {
  if (reponse.status === 401) {
    return new ErreurAppel('Session expirée. Reconnectez-vous pour ' + action + '.', 401);
  }
  if (reponse.status === 403) {
    return new ErreurAppel("Vous n'avez pas la permission de " + action + '.', 403);
  }
  return null;
}

/**
 * Envoie un corps JSON en POST avec le jeton antiforgery de la page (input __RequestVerificationToken).
 * Un refus métier (400) lève une ErreurAppel dont `erreurs` liste les messages du serveur.
 */
export async function envoyerJson(url, corps) {
  const jeton = document.querySelector('input[name="__RequestVerificationToken"]');
  const reponse = await fetch(url, {
    method: 'POST',
    headers: { ...enTetesJson, 'Content-Type': 'application/json', RequestVerificationToken: jeton ? jeton.value : '' },
    body: JSON.stringify(corps),
  });

  const erreurDroits = erreurAuthentification(reponse, 'enregistrer ces données');
  if (erreurDroits) {
    throw erreurDroits;
  }
  if (reponse.status === 400) {
    const contenu = await reponse.json().catch(() => ({}));
    const erreurs = Array.isArray(contenu.erreurs) ? contenu.erreurs : ['Les données transmises sont invalides.'];
    const erreur = new ErreurAppel(erreurs.join(' '), 400);
    erreur.erreurs = erreurs;
    throw erreur;
  }
  if (!reponse.ok) {
    throw new ErreurAppel('Le serveur a répondu ' + reponse.status + '.', reponse.status);
  }
}

export async function obtenirJson(url, options = {}) {
  const reponse = await fetch(url, { ...options, headers: { ...enTetesJson, ...options.headers } });

  const erreurDroits = erreurAuthentification(reponse, 'afficher ces données');
  if (erreurDroits) {
    throw erreurDroits;
  }
  if (!reponse.ok) {
    throw new ErreurAppel('Le serveur a répondu ' + reponse.status + '.', reponse.status);
  }

  // Filet de sécurité : page de connexion HTML renvoyée à la place du JSON.
  if (!(reponse.headers.get('Content-Type') ?? '').includes('application/json')) {
    throw new ErreurAppel('Réponse inattendue du serveur (session probablement expirée).', reponse.status);
  }

  return await reponse.json();
}
