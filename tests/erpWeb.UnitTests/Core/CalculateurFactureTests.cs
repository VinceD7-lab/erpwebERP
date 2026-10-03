using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.UnitTests.Outils;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace erpWeb.UnitTests.Core;

public sealed class CalculateurFactureTests
{
    private static readonly DateTime _datePrelevementRecente = new(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _horloge = new(BaseDonneesTest.DateReference);

    [Theory]
    [InlineData("Grandes cultures", 45)]
    [InlineData("Elevage", 38)]
    [InlineData("Maraichage", 52)]
    [InlineData("Viticulture", 60)]
    public void Calculer_FiliereConnue_AppliqueLeTarifDeLaFiliere(string filiere, decimal tarifAttendu)
    {
        var facture = CreerCalculateur(new OptionsFacturation { TauxTva = 0m }).Calculer(CreerEchantillon(filiere: filiere));

        Assert.Equal(tarifAttendu, facture.MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_FiliereDeCasseDifferente_AppliqueLeTarifDeLaFiliere()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon(filiere: "VITICULTURE"));

        Assert.Equal(60m, facture.MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_FiliereInconnue_AppliqueLeTarifParDefaut()
    {
        var options = new OptionsFacturation { TarifParDefaut = 33m };

        var facture = CreerCalculateur(options).Calculer(CreerEchantillon(filiere: "Apiculture"));

        Assert.Equal(33m, facture.MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_SansRemise_ExposeUneRemiseNulle()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon());

        Assert.Null(facture.Remise);
        Assert.Equal(45m, facture.MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_RemiseNulle_ExposeUneRemiseNulle()
    {
        var facture = CreerCalculateur(new OptionsFacturation { RemisePourcentage = 0m }).Calculer(CreerEchantillon());

        Assert.Null(facture.Remise);
    }

    [Fact]
    public void Calculer_AvecRemise_ReduitLeMontantHorsTaxeEtLeConserve()
    {
        var facture = CreerCalculateur(new OptionsFacturation { RemisePourcentage = 10m }).Calculer(CreerEchantillon());

        Assert.Equal(40.50m, facture.MontantHorsTaxe);
        Assert.Equal(10m, facture.Remise);
        Assert.Equal(8.10m, facture.MontantTva);
        Assert.Equal(48.60m, facture.MontantToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_TauxTvaParDefaut_CalculeLaTvaSurLeMontantHorsTaxe()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon());

        Assert.Equal(20m, facture.TauxTva);
        Assert.Equal(9m, facture.MontantTva);
        Assert.Equal(54m, facture.MontantToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_TauxTvaNul_ToutesTaxesComprisesEgalHorsTaxe()
    {
        var facture = CreerCalculateur(new OptionsFacturation { TauxTva = 0m }).Calculer(CreerEchantillon());

        Assert.Equal(0m, facture.MontantTva);
        Assert.Equal(facture.MontantHorsTaxe, facture.MontantToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_MontantHorsTaxeAuDemiCentime_EstArrondiAuCentimeSuperieur()
    {
        var options = new OptionsFacturation { TarifParDefaut = 10.025m, TauxTva = 0m };

        var facture = CreerCalculateur(options).Calculer(CreerEchantillon(filiere: "Inconnue"));

        Assert.Equal(10.03m, facture.MontantHorsTaxe);
    }

    [Fact]
    public void Calculer_TvaAuDessusDuDemiCentime_EstArrondieAuCentimeSuperieur()
    {
        var options = new OptionsFacturation { TarifParDefaut = 10.10m, TauxTva = 5.5m };

        var facture = CreerCalculateur(options).Calculer(CreerEchantillon(filiere: "Inconnue"));

        // 10,10 x 5,5 % = 0,5555 -> 0,56
        Assert.Equal(0.56m, facture.MontantTva);
        Assert.Equal(10.66m, facture.MontantToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_TvaExactementAuDemiCentime_EstArrondieLoinDeZero()
    {
        var options = new OptionsFacturation { TarifParDefaut = 0.25m, TauxTva = 10m };

        var facture = CreerCalculateur(options).Calculer(CreerEchantillon(filiere: "Inconnue"));

        // 0,25 x 10 % = 0,025 -> 0,03 (et non 0,02 comme l'arrondi bancaire)
        Assert.Equal(0.03m, facture.MontantTva);
    }

    [Theory]
    [InlineData(10.025, 20, 5.5)]
    [InlineData(33.33, 7, 0)]
    [InlineData(45, 20, 12.5)]
    public void Calculer_QuelsQueSoientLesMontants_ToutesTaxesComprisesEgaleHorsTaxePlusTva(double tarif, double tauxTva, double remise)
    {
        var options = new OptionsFacturation { TarifParDefaut = (decimal)tarif, TauxTva = (decimal)tauxTva, RemisePourcentage = (decimal)remise };

        var facture = CreerCalculateur(options).Calculer(CreerEchantillon(filiere: "Inconnue"));

        Assert.Equal(facture.MontantHorsTaxe + facture.MontantTva, facture.MontantToutesTaxesComprises);
    }

    [Fact]
    public void Calculer_ResultatValide_UtiliseLaDateDeValidationCommeDateDeFacture()
    {
        var echantillon = CreerEchantillon();
        echantillon.ResultatAgronomie = new ResultatAgronomie { DateValidation = new DateTime(2026, 9, 12, 23, 30, 0, DateTimeKind.Utc) };

        var facture = CreerCalculateur().Calculer(echantillon);

        Assert.Equal(new DateOnly(2026, 9, 12), facture.DateFacture);
    }

    [Fact]
    public void Calculer_ResultatSansDateDeValidation_UtiliseLaDateDePrelevement()
    {
        var echantillon = CreerEchantillon();
        echantillon.ResultatAgronomie = new ResultatAgronomie { DateValidation = null };

        var facture = CreerCalculateur().Calculer(echantillon);

        Assert.Equal(new DateOnly(2026, 9, 10), facture.DateFacture);
    }

    [Fact]
    public void Calculer_SansResultat_UtiliseLaDateDePrelevement()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon());

        Assert.Equal(new DateOnly(2026, 9, 10), facture.DateFacture);
    }

    [Fact]
    public void Calculer_DelaiParDefaut_EcheanceTrenteJoursApresLaDateDeFacture()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon());

        Assert.Equal(new DateOnly(2026, 10, 10), facture.DateEcheance);
    }

    [Fact]
    public void Calculer_DelaiPersonnalise_AjouteLeDelaiALaDateDeFacture()
    {
        var facture = CreerCalculateur(new OptionsFacturation { DelaiEcheanceEnJours = 45 }).Calculer(CreerEchantillon());

        Assert.Equal(new DateOnly(2026, 10, 25), facture.DateEcheance);
    }

    [Fact]
    public void Calculer_EcheanceFuture_StatutEmise()
    {
        var facture = CreerCalculateur().Calculer(CreerEchantillon());

        Assert.Equal(StatutsFacture.Emise, facture.StatutFacture);
    }

    [Fact]
    public void Calculer_EcheanceAujourdhui_StatutEmise()
    {
        var echantillon = CreerEchantillon(datePrelevement: new DateTime(2026, 8, 16, 8, 0, 0, DateTimeKind.Utc));

        var facture = CreerCalculateur().Calculer(echantillon);

        Assert.Equal(new DateOnly(2026, 9, 15), facture.DateEcheance);
        Assert.Equal(StatutsFacture.Emise, facture.StatutFacture);
    }

    [Fact]
    public void Calculer_EcheanceDepassee_StatutEnRetard()
    {
        var echantillon = CreerEchantillon(datePrelevement: new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc));

        var facture = CreerCalculateur().Calculer(echantillon);

        Assert.Equal(new DateOnly(2026, 9, 14), facture.DateEcheance);
        Assert.Equal(StatutsFacture.EnRetard, facture.StatutFacture);
    }

    [Fact]
    public void Calculer_HorlogeAvancee_BasculeEnRetardAuLendemainDeLEcheance()
    {
        var calculateur = CreerCalculateur();
        var echantillon = CreerEchantillon();

        // Échéance : 2026-10-10 ; l'horloge démarre le 2026-09-15.
        _horloge.Advance(TimeSpan.FromDays(25));
        var jourDeLEcheance = calculateur.Calculer(echantillon);
        _horloge.Advance(TimeSpan.FromDays(1));
        var lendemain = calculateur.Calculer(echantillon);

        Assert.Equal(StatutsFacture.Emise, jourDeLEcheance.StatutFacture);
        Assert.Equal(StatutsFacture.EnRetard, lendemain.StatutFacture);
    }

    [Fact]
    public void Calculer_Echantillon_RecopieLesInformationsFigeesDuClient()
    {
        var client = new Client
        {
            Id = 7,
            RaisonSociale = "Acier SA",
            Adresse1 = "12 rue des Forges",
            CodePostal = "69001",
            Ville = "Lyon",
            Pays = "France",
        };
        var echantillon = CreerEchantillon(client: client, id: 42);

        var facture = CreerCalculateur(new OptionsFacturation { Devise = "EUR", ModePaiementParDefaut = "Cheque" }).Calculer(echantillon);

        Assert.Equal(42, facture.IdEchantillon);
        Assert.Equal(7, facture.IdClient);
        Assert.Equal("Acier SA", facture.NomClient);
        Assert.Equal("12 rue des Forges", facture.AdresseFacturation);
        Assert.Equal("69001", facture.CodePostal);
        Assert.Equal("Lyon", facture.Ville);
        Assert.Equal("France", facture.Pays);
        Assert.Equal("Cheque", facture.ModePaiement);
        Assert.Equal("EUR", facture.Devise);
        Assert.Equal(0, facture.NumeroFacture);
    }

    [Fact]
    public void Calculer_DeuxLignesDAdresse_LesConcateneAvecUneVirgule()
    {
        var client = new Client { RaisonSociale = "Acier SA", Adresse1 = " 12 rue des Forges ", Adresse2 = "Batiment B" };

        var facture = CreerCalculateur().Calculer(CreerEchantillon(client: client));

        Assert.Equal("12 rue des Forges, Batiment B", facture.AdresseFacturation);
    }

    [Fact]
    public void Calculer_SeulementLaSecondeLigneDAdresse_NeLaisseAucuneVirguleParasite()
    {
        var client = new Client { RaisonSociale = "Acier SA", Adresse1 = "  ", Adresse2 = "Batiment B" };

        var facture = CreerCalculateur().Calculer(CreerEchantillon(client: client));

        Assert.Equal("Batiment B", facture.AdresseFacturation);
    }

    [Fact]
    public void Calculer_AucuneLigneDAdresse_ExposeUneAdresseNulle()
    {
        var client = new Client { RaisonSociale = "Acier SA", Adresse1 = null, Adresse2 = " " };

        var facture = CreerCalculateur().Calculer(CreerEchantillon(client: client));

        Assert.Null(facture.AdresseFacturation);
    }

    [Fact]
    public void Calculer_ChampsClientTropLongs_SontTronquesAuxLongueursDeLaColonne()
    {
        var client = new Client
        {
            RaisonSociale = new string('R', LongueursFacture.NomClient + 20),
            Adresse1 = new string('A', LongueursFacture.AdresseFacturation + 20),
            CodePostal = new string('9', LongueursFacture.CodePostal + 5),
            Ville = new string('V', LongueursFacture.Ville + 5),
            Pays = new string('P', LongueursFacture.Pays + 5),
        };

        var facture = CreerCalculateur().Calculer(CreerEchantillon(client: client));

        Assert.Equal(LongueursFacture.NomClient, facture.NomClient.Length);
        Assert.Equal(LongueursFacture.AdresseFacturation, facture.AdresseFacturation!.Length);
        Assert.Equal(LongueursFacture.CodePostal, facture.CodePostal!.Length);
        Assert.Equal(LongueursFacture.Ville, facture.Ville!.Length);
        Assert.Equal(LongueursFacture.Pays, facture.Pays!.Length);
    }

    [Fact]
    public void Calculer_AdresseDeLongueurExactementMaximale_NEstPasModifiee()
    {
        var adresse = new string('A', LongueursFacture.AdresseFacturation);
        var client = new Client { RaisonSociale = "Acier SA", Adresse1 = adresse };

        var facture = CreerCalculateur().Calculer(CreerEchantillon(client: client));

        Assert.Equal(adresse, facture.AdresseFacturation);
    }

    [Fact]
    public void Calculer_EchantillonSansClient_LeveArgumentException()
    {
        var echantillon = CreerEchantillon();
        echantillon.Client = null;
        echantillon.IdClient = null;
        var calculateur = CreerCalculateur();

        var exception = Assert.Throws<ArgumentException>(() => calculateur.Calculer(echantillon));

        Assert.Equal("echantillon", exception.ParamName);
    }

    private CalculateurFacture CreerCalculateur(OptionsFacturation? options = null)
        => new(Options.Create(options ?? new OptionsFacturation()), _horloge);

    private static Echantillon CreerEchantillon(
        string filiere = "Grandes cultures",
        DateTime? datePrelevement = null,
        Client? client = null,
        int id = 1)
        => new()
        {
            Id = id,
            CodeBarresAnonyme = $"ECH-{id:D5}",
            Filiere = filiere,
            DatePrelevement = datePrelevement ?? _datePrelevementRecente,
            Client = client ?? new Client { Id = 1, RaisonSociale = "Acier SA" },
        };
}
