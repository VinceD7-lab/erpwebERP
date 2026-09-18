using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace erpWeb.IntegrationTests;

/// <summary>
/// Application complète sur une base SQL Server temporaire (migrations réelles), supprimée à la fin.
/// Serveur : variable d'environnement ERPWEB_TESTS_SQLSERVER (CI), sinon LocalDB.
/// L'environnement « Test » évite de charger les secrets utilisateur du poste de développement.
/// </summary>
public sealed class FabriqueApplication : WebApplicationFactory<Program>
{
    public const string EmailAdministrateur = "admin-tests@erpweb.local";
    public const string MotDePasseAdministrateur = "Admin#Tests2026";
    public const string VariableServeurTests = "ERPWEB_TESTS_SQLSERVER";

    private const string ServeurParDefaut = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _nomBase;
    private readonly string _chaineServeur;
    private readonly string _dossierDocuments;

    public FabriqueApplication()
    {
        var identifiant = Guid.NewGuid().ToString("N");
        _nomBase = $"erpWeb_Tests_{identifiant}";
        _chaineServeur = Environment.GetEnvironmentVariable(VariableServeurTests) ?? ServeurParDefaut;
        _dossierDocuments = Path.Combine(Path.GetTempPath(), $"erpWeb-tests-{identifiant}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var chaineConnexion = new SqlConnectionStringBuilder(_chaineServeur) { InitialCatalog = _nomBase }.ConnectionString;

        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:ParDefaut", chaineConnexion);
        builder.UseSetting("BaseDeDonnees:AppliquerMigrationsAuDemarrage", "true");
        builder.UseSetting("Administrateur:Email", EmailAdministrateur);
        builder.UseSetting("Administrateur:MotDePasse", MotDePasseAdministrateur);
        builder.UseSetting("Stockage:Dossier", _dossierDocuments);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SupprimerBaseDeTest();
            if (Directory.Exists(_dossierDocuments))
            {
                Directory.Delete(_dossierDocuments, recursive: true);
            }
        }
    }

    private void SupprimerBaseDeTest()
    {
        SqlConnection.ClearAllPools();

        var chaineMaster = new SqlConnectionStringBuilder(_chaineServeur) { InitialCatalog = "master" }.ConnectionString;
        using var connexion = new SqlConnection(chaineMaster);
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = $"""
            IF DB_ID(N'{_nomBase}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_nomBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_nomBase}];
            END
            """;
        commande.ExecuteNonQuery();
    }
}
