namespace erpWeb.Core.Clients;

/// <summary>Longueurs partagées par les validateurs et la configuration EF.</summary>
public static class LongueursClient
{
    public const int RaisonSociale = 100;
    public const int Adresse = 200;

    /// <summary>Couvre les formats étrangers (Royaume-Uni « SW1A 1AA », Canada « K1A 0B1 »).</summary>
    public const int CodePostal = 10;

    public const int Ville = 100;
    public const int Pays = 50;

    /// <summary>Longueur enregistrée, après retrait des espaces et des points.</summary>
    public const int Telephone = 12;

    /// <summary>Longueur acceptée dans le formulaire, séparateurs compris (« 01 02 03 04 05 »).</summary>
    public const int SaisieTelephone = 20;

    public const int Email = 100;
}
