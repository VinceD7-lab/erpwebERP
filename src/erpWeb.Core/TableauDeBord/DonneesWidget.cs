namespace erpWeb.Core.TableauDeBord;

/// <summary>
/// Données affichées par un widget. Chaque type concret possède une vue de rendu générique :
/// un nouveau widget réutilise un type existant sans nouvelle vue.
/// </summary>
public abstract record DonneesWidget;

public sealed record DonneesIndicateur(string Valeur, string Libelle, string? Detail) : DonneesWidget;

public sealed record DonneesGraphique(string TypeGraphique, IReadOnlyList<string> Etiquettes, IReadOnlyList<SerieGraphique> Series) : DonneesWidget;

public sealed record SerieGraphique(string Libelle, IReadOnlyList<int> Valeurs);

public sealed record DonneesListe(IReadOnlyList<ElementListe> Elements, string MessageVide) : DonneesWidget;

public sealed record ElementListe(string Libelle, string? Detail, string? Url);

public sealed record DonneesRaccourcis(IReadOnlyList<Raccourci> Raccourcis) : DonneesWidget;

public sealed record Raccourci(string Libelle, string Icone, string Url, string? PermissionRequise);
