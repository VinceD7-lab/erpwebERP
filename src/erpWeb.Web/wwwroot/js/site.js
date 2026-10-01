'use strict';

(function () {
  const langueTableaux = {
    search: 'Rechercher :',
    lengthMenu: 'Afficher _MENU_ éléments',
    info: 'Éléments _START_ à _END_ sur _TOTAL_',
    infoEmpty: 'Aucun élément',
    infoFiltered: '(filtrés sur _MAX_)',
    zeroRecords: 'Aucun élément correspondant',
    emptyTable: 'Aucune donnée disponible',
    paginate: { first: 'Premier', previous: 'Précédent', next: 'Suivant', last: 'Dernier' }
  };

  /** Crée les graphiques Chart.js décrits par l'attribut data-graphique (DonneesGraphique sérialisé). */
  function initialiserGraphiques(racine) {
    racine.querySelectorAll('canvas[data-graphique]').forEach(function (canvas) {
      const donnees = JSON.parse(canvas.dataset.graphique);
      const existant = Chart.getChart(canvas);
      if (existant) {
        existant.destroy();
      }

      new Chart(canvas, {
        type: donnees.typeGraphique,
        data: {
          labels: donnees.etiquettes,
          datasets: donnees.series.map(function (serie) {
            return { label: serie.libelle, data: serie.valeurs, tension: 0.3, fill: true };
          })
        },
        options: {
          maintainAspectRatio: false,
          plugins: { legend: { display: donnees.series.length > 1 } },
          scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
        }
      });
    });
  }

  const instancesTableaux = new WeakMap();

  /** Active DataTables sur les tableaux marqués data-tableau. */
  function initialiserTableaux(racine) {
    racine.querySelectorAll('table[data-tableau]').forEach(function (tableau) {
      instancesTableaux.set(tableau, new DataTable(tableau, { language: langueTableaux, order: [], pageLength: 25 }));
    });
  }

  async function rafraichirWidget(bouton) {
    const nom = bouton.dataset.rafraichirWidget;
    const contenu = document.querySelector('[data-contenu-widget="' + CSS.escape(nom) + '"]');
    bouton.disabled = true;
    contenu.setAttribute('aria-busy', 'true');

    try {
      const reponse = await fetch(document.body.dataset.racine + 'TableauDeBord/Widget/' + encodeURIComponent(nom), {
        headers: { 'X-Requested-With': 'fetch' }
      });
      if (!reponse.ok) {
        throw new Error('HTTP ' + reponse.status);
      }

      contenu.innerHTML = await reponse.text();
      initialiserGraphiques(contenu);
    } catch (erreur) {
      contenu.insertAdjacentHTML('afterbegin', '<div class="alert alert-warning py-1 small" role="alert">Rafraîchissement impossible.</div>');
    } finally {
      bouton.disabled = false;
      contenu.removeAttribute('aria-busy');
    }
  }

  // Édition d'une ligne de tableau sans rechargement : le serveur renvoie le <tr> (lecture ou édition) en HTML.
  // Attributs : data-editer-ligne (lien, repli sur son href), data-enregistrer-ligne, data-annuler-ligne,
  // chacun portant data-url ; data-messages-ligne (zone aria-live) ; jeton antiforgery présent dans la page.
  const delaiMessageLigne = 5000;
  let minuterieMessageLigne;

  function afficherMessageLigne(type, texte) {
    const zone = document.querySelector('[data-messages-ligne]');
    if (!zone) {
      return;
    }

    clearTimeout(minuterieMessageLigne);
    const alerte = document.createElement('div');
    alerte.className = 'alert alert-' + type + ' py-2 small';
    alerte.setAttribute('role', type === 'danger' ? 'alert' : 'status');
    alerte.textContent = texte;
    zone.replaceChildren(alerte);

    if (type === 'success') {
      minuterieMessageLigne = setTimeout(function () { alerte.remove(); }, delaiMessageLigne);
    }
  }

  /** Appelle une action « fragment » et retourne le <tr> reçu ; valide vaut false pour un 422 (erreurs de saisie). */
  async function demanderLigne(url, options) {
    const reponse = await fetch(url, { ...options, headers: { 'X-Requested-With': 'fetch', ...options?.headers } });
    if (reponse.redirected || reponse.status === 401) {
      throw new Error('Session expirée. Reconnectez-vous puis réessayez.');
    }
    if (reponse.status === 403) {
      throw new Error("Vous n'avez pas la permission de modifier cette ligne.");
    }
    if (!reponse.ok && reponse.status !== 422) {
      throw new Error('Le serveur a répondu ' + reponse.status + '.');
    }

    const modele = document.createElement('template');
    modele.innerHTML = (await reponse.text()).trim();
    const ligne = modele.content.querySelector('tr[data-ligne-editable]');
    if (!ligne) {
      throw new Error('Réponse inattendue du serveur.');
    }

    return { ligne: ligne, valide: reponse.ok };
  }

  /** Remplace le contenu de la ligne en gardant son nœud <tr>, dont DataTables conserve la référence. */
  function appliquerLigne(ligne, nouvelleLigne) {
    ligne.className = nouvelleLigne.className;
    ligne.dataset.id = nouvelleLigne.dataset.id;
    ligne.replaceChildren(...nouvelleLigne.children);
  }

  /** Relit la ligne dans le DOM pour que tri, recherche et pagination tiennent compte des nouvelles valeurs. */
  function synchroniserTableau(ligne) {
    const tableau = instancesTableaux.get(ligne.closest('table'));
    if (tableau) {
      tableau.row(ligne).invalidate('dom').draw(false);
    }
  }

  function focaliserPremierChamp(ligne) {
    const champ = ligne.querySelector('input:not([type="hidden"])');
    if (champ) {
      champ.focus();
    }
  }

  async function ouvrirEditionLigne(lien) {
    const ligne = lien.closest('tr');
    ligne.setAttribute('aria-busy', 'true');

    try {
      const resultat = await demanderLigne(lien.dataset.url);
      appliquerLigne(ligne, resultat.ligne);
      focaliserPremierChamp(ligne);
    } catch (erreur) {
      // Repli : écran de modification complet.
      window.location.assign(lien.href);
    } finally {
      ligne.removeAttribute('aria-busy');
    }
  }

  async function annulerEditionLigne(bouton) {
    const ligne = bouton.closest('tr');
    ligne.setAttribute('aria-busy', 'true');

    try {
      const resultat = await demanderLigne(bouton.dataset.url);
      appliquerLigne(ligne, resultat.ligne);
      synchroniserTableau(ligne);
    } catch (erreur) {
      afficherMessageLigne('danger', erreur.message);
    } finally {
      ligne.removeAttribute('aria-busy');
    }
  }

  async function enregistrerLigne(bouton) {
    const ligne = bouton.closest('tr');
    const donnees = new FormData();
    ligne.querySelectorAll('input[name]').forEach(function (champ) {
      donnees.append(champ.name, champ.value);
    });
    const jeton = document.querySelector('input[name="__RequestVerificationToken"]');

    bouton.disabled = true;
    ligne.setAttribute('aria-busy', 'true');

    try {
      const resultat = await demanderLigne(bouton.dataset.url, {
        method: 'POST',
        body: donnees,
        headers: { RequestVerificationToken: jeton ? jeton.value : '' }
      });
      appliquerLigne(ligne, resultat.ligne);

      if (resultat.valide) {
        synchroniserTableau(ligne);
        afficherMessageLigne('success', 'Les modifications de « ' + ligne.cells[0].textContent.trim() + ' » ont été enregistrées.');
      } else {
        focaliserPremierChamp(ligne);
      }
    } catch (erreur) {
      bouton.disabled = false;
      afficherMessageLigne('danger', erreur.message);
    } finally {
      ligne.removeAttribute('aria-busy');
    }
  }

  document.addEventListener('click', function (evenement) {
    const bouton = evenement.target.closest('[data-rafraichir-widget]');
    if (bouton) {
      rafraichirWidget(bouton);
      return;
    }

    const editer = evenement.target.closest('[data-editer-ligne]');
    if (editer) {
      evenement.preventDefault();
      ouvrirEditionLigne(editer);
      return;
    }

    const enregistrer = evenement.target.closest('[data-enregistrer-ligne]');
    if (enregistrer) {
      enregistrerLigne(enregistrer);
      return;
    }

    const annuler = evenement.target.closest('[data-annuler-ligne]');
    if (annuler) {
      annulerEditionLigne(annuler);
    }
  });

  // Entrée enregistre, Échap annule, uniquement sur une ligne en cours d'édition.
  document.addEventListener('keydown', function (evenement) {
    const ligne = evenement.target.closest('tr[data-ligne-editable]');
    const enregistrer = ligne && ligne.querySelector('[data-enregistrer-ligne]');
    if (!enregistrer) {
      return;
    }

    if (evenement.key === 'Escape') {
      evenement.preventDefault();
      ligne.querySelector('[data-annuler-ligne]').click();
    } else if (evenement.key === 'Enter' && evenement.target.matches('input')) {
      evenement.preventDefault();
      enregistrer.click();
    }
  });

  // Confirmation des actions sensibles : <form data-confirmation="Message ?">
  document.addEventListener('submit', function (evenement) {
    const message = evenement.target.dataset.confirmation;
    if (message && !window.confirm(message)) {
      evenement.preventDefault();
    }
  });

  document.addEventListener('DOMContentLoaded', function () {
    initialiserGraphiques(document);
    initialiserTableaux(document);
  });
})();
