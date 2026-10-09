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
      const options = { language: langueTableaux, order: [], pageLength: 25 };
      // data-hauteur-defilement="29rem" : seul le corps du tableau défile, la recherche, le choix du nombre
      // d'éléments, l'en-tête et la pagination restent affichés.
      if (tableau.dataset.hauteurDefilement) {
        options.scrollY = tableau.dataset.hauteurDefilement;
        options.scrollCollapse = true;
      }
      instancesTableaux.set(tableau, new DataTable(tableau, options));
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

  // Sélection d'une ligne (clic gauche ou Entrée/Espace) : charge le détail du client sous le tableau puis annonce
  // la sélection par l'événement « client-selectionne » (detail.idClient), écouté par le tableau des échantillons.
  // Attributs : section[data-detail-client][data-url-detail], [data-detail-client-contenu].
  const selecteursInteractifs = 'a, button, input, select, textarea, form';
  let numeroSelection = 0;

  function lireDetailClient() {
    return document.querySelector('[data-detail-client]');
  }

  async function chargerDetailClient(section, idClient) {
    const numero = ++numeroSelection;
    const contenu = section.querySelector('[data-detail-client-contenu]');
    contenu.setAttribute('aria-busy', 'true');

    try {
      const reponse = await fetch(section.dataset.urlDetail + '?id=' + encodeURIComponent(idClient), {
        headers: { 'X-Requested-With': 'fetch' }
      });
      if (reponse.redirected || reponse.status === 401) {
        throw new Error('Session expirée. Reconnectez-vous puis réessayez.');
      }
      if (!reponse.ok) {
        throw new Error('Le serveur a répondu ' + reponse.status + '.');
      }

      const html = await reponse.text();
      // Une sélection plus récente a pris le relais : cette réponse est périmée.
      if (numero === numeroSelection) {
        contenu.innerHTML = html;
      }
    } catch (erreur) {
      if (numero === numeroSelection) {
        const alerte = document.createElement('div');
        alerte.className = 'alert alert-warning py-2 small';
        alerte.setAttribute('role', 'alert');
        alerte.textContent = erreur.message;
        contenu.replaceChildren(alerte);
      }
    } finally {
      if (numero === numeroSelection) {
        contenu.removeAttribute('aria-busy');
      }
    }
  }

  function selectionnerLigne(ligne) {
    const section = lireDetailClient();
    if (!section || ligne.querySelector('[data-enregistrer-ligne]')) {
      return;
    }

    // aria-selected sert de marqueur (et de style, voir site.css) : la classe table-active est déjà utilisée
    // par le gabarit d'une ligne en cours d'édition ; attribut non touché quand la ligne est remplacée.
    ligne.closest('table').querySelectorAll('tr[aria-selected="true"]').forEach(function (selection) {
      selection.removeAttribute('aria-selected');
    });
    ligne.setAttribute('aria-selected', 'true');

    // Affichée avant l'événement : la grille des échantillons doit mesurer une zone visible.
    section.hidden = false;
    chargerDetailClient(section, ligne.dataset.id);
    document.dispatchEvent(new CustomEvent('client-selectionne', { detail: { idClient: ligne.dataset.id } }));
  }

  /** Recharge le détail si la ligne modifiée est celle qui est sélectionnée. */
  function rafraichirDetailSiSelectionnee(ligne) {
    const section = lireDetailClient();
    if (section && ligne.getAttribute('aria-selected') === 'true') {
      chargerDetailClient(section, ligne.dataset.id);
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
        rafraichirDetailSiSelectionnee(ligne);
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

  // Menu latéral : sur grand écran il se masque (état mémorisé), sur petit écran il s'ouvre en panneau (offcanvas).
  const cleMenuLateralMasque = 'menu-lateral-masque';
  const ecranLarge = window.matchMedia('(min-width: 992px)');

  function lireMenuLateralMasque() {
    try {
      return localStorage.getItem(cleMenuLateralMasque) === 'true';
    } catch (erreur) {
      return false;
    }
  }

  function appliquerMenuLateralMasque(masque) {
    document.body.classList.toggle('menu-lateral-masque', masque);
    const bouton = document.querySelector('[data-basculer-menu]');
    if (bouton) {
      bouton.setAttribute('aria-expanded', String(!masque));
    }
  }

  function basculerMenuLateral() {
    if (!ecranLarge.matches) {
      bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('barreLaterale')).toggle();
      return;
    }

    const masque = !document.body.classList.contains('menu-lateral-masque');
    appliquerMenuLateralMasque(masque);
    try {
      localStorage.setItem(cleMenuLateralMasque, String(masque));
    } catch (erreur) {
      // Stockage indisponible : le choix vaut pour la page courante seulement.
    }
  }

  appliquerMenuLateralMasque(lireMenuLateralMasque());

  document.addEventListener('click', function (evenement) {
    if (evenement.target.closest('[data-basculer-menu]')) {
      basculerMenuLateral();
      return;
    }

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
      return;
    }

    const ligneCliquee = evenement.target.closest('tr[data-ligne-editable]');
    if (ligneCliquee && !evenement.target.closest(selecteursInteractifs)) {
      selectionnerLigne(ligneCliquee);
      return;
    }

    // Impression de la page courante (la mise en page d'impression est définie par @media print dans site.css).
    if (evenement.target.closest('[data-imprimer]')) {
      window.print();
    }
  });

  // Entrée enregistre, Échap annule, uniquement sur une ligne en cours d'édition.
  document.addEventListener('keydown', function (evenement) {
    const ligne = evenement.target.closest('tr[data-ligne-editable]');
    if (ligne && evenement.target === ligne && (evenement.key === 'Enter' || evenement.key === ' ')) {
      evenement.preventDefault();
      selectionnerLigne(ligne);
      return;
    }

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

  /** Rend les lignes atteignables au clavier lorsque la page affiche un détail de ligne. */
  function initialiserLignesSelectionnables(racine) {
    if (!lireDetailClient()) {
      return;
    }

    racine.querySelectorAll('tr[data-ligne-editable]').forEach(function (ligne) {
      ligne.tabIndex = 0;
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initialiserGraphiques(document);
    initialiserTableaux(document);
    initialiserLignesSelectionnables(document);
  });
})();
