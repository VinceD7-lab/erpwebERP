namespace erpWeb.Core.Communs;

public sealed record ResultatOperation(bool Reussi, IReadOnlyList<string> Erreurs)
{
    public static ResultatOperation Succes() => new(true, []);

    public static ResultatOperation Echec(params string[] erreurs) => new(false, erreurs);

    public static ResultatOperation Echec(IEnumerable<string> erreurs) => new(false, erreurs.ToArray());
}

public sealed record ResultatOperation<T>(bool Reussi, T? Valeur, IReadOnlyList<string> Erreurs)
{
    public static ResultatOperation<T> Succes(T valeur) => new(true, valeur, []);

    public static ResultatOperation<T> Echec(params string[] erreurs) => new(false, default, erreurs);

    public static ResultatOperation<T> Echec(IEnumerable<string> erreurs) => new(false, default, erreurs.ToArray());
}
