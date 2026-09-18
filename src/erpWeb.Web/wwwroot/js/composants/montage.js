'use strict';

// Monte les îlots Vue déclarés par les vues Razor :
//   <div data-composant-vue="nom-du-composant" data-…="…">contenu de repli</div>
// Convention (Open/Closed) : le composant « nom-du-composant » est le module
// wwwroot/js/composants/nom-du-composant.js, qui exporte par défaut ses options Vue.
// Ajouter un composant = ajouter un fichier ; ce noyau n'est jamais modifié.

const dossierComposants = new URL('.', import.meta.url);
const nomValide = /^[a-z][a-z0-9-]*$/;

/** Les attributs data-* (hors data-composant-vue) deviennent les propriétés du composant, toujours en chaînes. */
function lireProprietes(element) {
  const proprietes = { ...element.dataset };
  delete proprietes.composantVue;
  return proprietes;
}

function signalerErreur(element, erreur) {
  console.error('Îlot Vue « %s » : %o', element.dataset.composantVue, erreur);
  element.textContent = '';
  element.insertAdjacentHTML('afterbegin',
    '<div class="alert alert-warning" role="alert">Ce composant n\'a pas pu être chargé.</div>');
}

async function monter(element) {
  const nom = element.dataset.composantVue;

  // Sans cette validation, un nom tel que « ../../../quelque-chose » provoquerait un import arbitraire.
  if (!nomValide.test(nom)) {
    throw new Error('Nom de composant invalide : ' + nom);
  }

  const module = await import(new URL(nom + '.js', dossierComposants).href);
  Vue.createApp(module.default, lireProprietes(element)).mount(element);
}

/** Monte tous les îlots présents sous la racine donnée (document entier par défaut). */
export async function monterComposants(racine = document) {
  const elements = [...racine.querySelectorAll('[data-composant-vue]')];

  await Promise.all(elements.map(async (element) => {
    try {
      await monter(element);
    } catch (erreur) {
      signalerErreur(element, erreur);
    }
  }));
}

if (window.Vue) {
  // Un module est différé : le DOM est déjà analysé quand ce code s'exécute.
  monterComposants();
} else {
  console.error('Vue 3 absent : ajouter <partial name="_ScriptsVue" /> dans la section Scripts de la vue.');
}
