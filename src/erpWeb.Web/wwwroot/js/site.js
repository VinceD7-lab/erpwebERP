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

  /** Active DataTables sur les tableaux marqués data-tableau. */
  function initialiserTableaux(racine) {
    racine.querySelectorAll('table[data-tableau]').forEach(function (tableau) {
      new DataTable(tableau, { language: langueTableaux, order: [], pageLength: 25 });
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

  document.addEventListener('click', function (evenement) {
    const bouton = evenement.target.closest('[data-rafraichir-widget]');
    if (bouton) {
      rafraichirWidget(bouton);
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
