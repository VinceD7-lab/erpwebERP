namespace erpWeb.Core.Clients;

/// <summary>
/// Retire les séparateurs de saisie (espaces et points) d'un numéro de téléphone,
/// partagé par la validation et le mapping pour contrôler exactement la valeur enregistrée.
/// </summary>
internal static class NormalisationTelephone
{
    public static string? Normaliser(string? telephone)
    {
        if (telephone is null)
        {
            return null;
        }

        var normalise = string.Concat(telephone.Where(caractere => caractere != '.' && !char.IsWhiteSpace(caractere)));
        return normalise.Length == 0 ? null : normalise;
    }
}
