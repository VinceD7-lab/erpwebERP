using erpWeb.Infrastructure.Donnees.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace erpWeb.Infrastructure.Configuration;

/// <summary>
/// Source de configuration alimentée par la table Parametres : les options typées (IOptions&lt;T&gt;)
/// peuvent être surchargées en base sans redéploiement.
/// </summary>
public sealed class SourceConfigurationParametres : IConfigurationSource
{
    private readonly string _chaineConnexion;

    public SourceConfigurationParametres(string chaineConnexion)
    {
        _chaineConnexion = chaineConnexion;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new FournisseurConfigurationParametres(_chaineConnexion);
}

public sealed class FournisseurConfigurationParametres : ConfigurationProvider
{
    private const string Requete = "SELECT Cle, Valeur FROM [dbo].[" + ConfigurationParametre.NomTable + "]";

    private readonly string _chaineConnexion;

    public FournisseurConfigurationParametres(string chaineConnexion)
    {
        _chaineConnexion = chaineConnexion;
    }

    public override void Load()
    {
        var donnees = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var connexion = new SqlConnection(_chaineConnexion);
            connexion.Open();
            using var commande = connexion.CreateCommand();
            commande.CommandText = Requete;
            using var lecteur = commande.ExecuteReader();
            while (lecteur.Read())
            {
                donnees[lecteur.GetString(0)] = lecteur.GetString(1);
            }
        }
        catch (SqlException)
        {
            // Base ou table absente (avant la première migration) : aucun paramètre en base.
        }

        Data = donnees;
    }
}
