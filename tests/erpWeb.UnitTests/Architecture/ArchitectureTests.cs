using System.Reflection;
using erpWeb.Core.Communs;
using erpWeb.Infrastructure.Donnees;
using erpWeb.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace erpWeb.UnitTests.Architecture;

/// <summary>
/// Vérifie automatiquement les règles d'architecture du socle (dépendances entre couches,
/// inversion de dépendances, absence d'accès direct au système de fichiers dans Core).
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly _core = typeof(IAppDbContext).Assembly;
    private static readonly Assembly _infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly _web = typeof(TableauDeBordController).Assembly;

    [Theory]
    [InlineData("erpWeb.Infrastructure")]
    [InlineData("erpWeb.Web")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData("Microsoft.Data.SqlClient")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData("Microsoft.Data.Sqlite")]
    [InlineData("MailKit")]
    [InlineData("MimeKit")]
    [InlineData("ClosedXML")]
    public void Core_NeReferencePasLAssemblage(string assemblageInterdit)
    {
        var references = _core.GetReferencedAssemblies().Select(reference => reference.Name);

        Assert.DoesNotContain(assemblageInterdit, references);
    }

    [Fact]
    public void Core_NeDependPasDesAutresCouches()
    {
        var resultat = Types.InAssembly(_core)
            .ShouldNot()
            .HaveDependencyOnAny("erpWeb.Infrastructure", "erpWeb.Web")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Core_NAccedePasDirectementAuSystemeDeFichiers()
    {
        var resultat = Types.InAssembly(_core)
            .ShouldNot()
            .HaveDependencyOnAny("System.IO.File", "System.IO.Directory", "System.Net.Http")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Web_NeDependDInfrastructureQueDansProgram()
    {
        var resultat = Types.InAssembly(_web)
            .That()
            .ResideInNamespace("erpWeb.Web")
            .ShouldNot()
            .HaveDependencyOn("erpWeb.Infrastructure")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Controleurs_NAccedentPasDirectementAuxDonnees()
    {
        var resultat = Types.InAssembly(_web)
            .That()
            .Inherit(typeof(Controller))
            .ShouldNot()
            .HaveDependencyOnAny(typeof(IAppDbContext).FullName!, "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertReussi(resultat);
    }

    [Fact]
    public void Infrastructure_SeulAppDbContextImplementeIAppDbContext()
    {
        var implementations = Types.InAssemblies([_core, _infrastructure, _web])
            .That()
            .ImplementInterface(typeof(IAppDbContext))
            .GetTypes()
            .ToList();

        Assert.Equal([typeof(AppDbContext)], implementations);
    }

    [Fact]
    public void Core_LesInterfacesSontPrefixeesParI()
    {
        var resultat = Types.InAssembly(_core)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        AssertReussi(resultat);
    }

    private static void AssertReussi(TestResult resultat)
        => Assert.True(resultat.IsSuccessful, "Types en infraction : " + string.Join(", ", resultat.FailingTypeNames ?? []));
}
