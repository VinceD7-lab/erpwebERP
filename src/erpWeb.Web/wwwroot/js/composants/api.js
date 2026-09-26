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

export async function obtenirJson(url, options = {}) {
  const reponse = await fetch(url, { ...options, headers: { ...enTetesJson, ...options.headers } });

  if (reponse.status === 401) {
    throw new ErreurAppel('Session expirée. Reconnectez-vous pour afficher ces données.', 401);
  }
  if (reponse.status === 403) {
    throw new ErreurAppel("Vous n'avez pas la permission de consulter ces données.", 403);
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
