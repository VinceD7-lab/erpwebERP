using erpWeb.Core.Communs;
using erpWeb.Infrastructure.Donnees;
using Mapster;
using MapsterMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace erpWeb.UnitTests.Outils;

/// <summary>
/// Base SQLite en mémoire (préférable au mock des DbSet) avec l'intercepteur d'audit,
/// une horloge contrôlée et un utilisateur courant simulé.
/// </summary>
public sealed class BaseDonneesTest : IDisposable
{
    public const string NomUtilisateurTest = "testeur@erpweb.local";

    public static readonly DateTimeOffset DateReference = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connexion;

    public BaseDonneesTest()
    {
        _connexion = new SqliteConnection("DataSource=:memory:");
        _connexion.Open();

        Horloge = new FakeTimeProvider(DateReference);
        UtilisateurCourant = new Mock<IUtilisateurCourant>();
        UtilisateurCourant.SetupGet(utilisateur => utilisateur.NomUtilisateur).Returns(NomUtilisateurTest);

        Contexte = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connexion)
            .AddInterceptors(new IntercepteurAudit(UtilisateurCourant.Object, Horloge))
            .Options);
        Contexte.Database.EnsureCreated();
    }

    public FakeTimeProvider Horloge { get; }

    public Mock<IUtilisateurCourant> UtilisateurCourant { get; }

    public AppDbContext Contexte { get; }

    public static IMapper CreerMapper()
    {
        var configuration = new TypeAdapterConfig();
        configuration.Scan(typeof(global::erpWeb.Core.DependencyInjection).Assembly);
        return new Mapper(configuration);
    }

    public void Dispose()
    {
        Contexte.Dispose();
        _connexion.Dispose();
    }
}
