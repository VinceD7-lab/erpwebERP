using erpWeb.Core.Clients;
using erpWeb.Core.DevisClients;
using erpWeb.UnitTests.Outils;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceRechercheDevisTests : IDisposable
{
    private static readonly DateTime _dateBase = new(2026, 9, 10);

    private readonly BaseDonneesTest _base = new();
    private int _sequence;

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task RechercherAsync_SansCritere_RetourneTousLesDevisAvecLeTotal()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterDevisAsync(client);
        await AjouterDevisAsync(client);
        await AjouterDevisAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresDevis());

        Assert.Equal(3, resultat.NombreTotal);
        Assert.Equal(3, resultat.Elements.Count);
        Assert.Equal(1, resultat.NumeroPage);
        Assert.Equal(CriteresDevis.TaillePageParDefaut, resultat.TaillePage);
    }

    [Fact]
    public async Task RechercherAsync_BaseVide_RetourneUnePageVide()
    {
        var resultat = await CreerService().RechercherAsync(new CriteresDevis());

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_Devis_ProjetteLesValeursDuDevisEtLaRaisonSocialeDuClient()
    {
        var client = await AjouterClientAsync("Acier SA");
        var devis = await AjouterDevisAsync(client, objet: "Audit des sols", reference: "CMD-12", statut: StatutDevis.Envoye, montantHorsTaxe: 100m);

        var resultat = await CreerService().RechercherAsync(new CriteresDevis());

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal(devis.Id, ligne.Id);
        Assert.Equal(devis.Numero, ligne.Numero);
        Assert.Equal(client.Id, ligne.ClientId);
        Assert.Equal("Acier SA", ligne.RaisonSocialeClient);
        Assert.Equal("Audit des sols", ligne.Objet);
        Assert.Equal("CMD-12", ligne.Reference);
        Assert.Equal(StatutDevis.Envoye, ligne.Statut);
        Assert.Equal(100m, ligne.MontantTotalHorsTaxe);
        Assert.Equal(120m, ligne.MontantTotalToutesTaxesComprises);
    }

    [Fact]
    public async Task RechercherAsync_TriParDefaut_OrdonneParRaisonSocialeCroissante()
    {
        var zebre = await AjouterClientAsync("Zébrures SARL");
        var acier = await AjouterClientAsync("Acier SA");
        var meunerie = await AjouterClientAsync("Meunerie Durand");
        await AjouterDevisAsync(zebre);
        await AjouterDevisAsync(acier);
        await AjouterDevisAsync(meunerie);

        var resultat = await CreerService().RechercherAsync(new CriteresDevis());

        Assert.Equal(["Acier SA", "Meunerie Durand", "Zébrures SARL"], resultat.Elements.Select(devis => devis.RaisonSocialeClient));
    }

    [Fact]
    public async Task RechercherAsync_MemeClient_OrdonneLesDevisDuPlusRecentAuPlusAncien()
    {
        var client = await AjouterClientAsync("Acier SA");
        var ancien = await AjouterDevisAsync(client, dateDevis: _dateBase);
        var recent = await AjouterDevisAsync(client, dateDevis: _dateBase.AddDays(5));

        var resultat = await CreerService().RechercherAsync(new CriteresDevis());

        Assert.Equal([recent.Id, ancien.Id], resultat.Elements.Select(devis => devis.Id));
    }

    [Fact]
    public async Task RechercherAsync_TriDescendantParObjet_OrdonneDeZAA()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterDevisAsync(client, objet: "Bêta");
        await AjouterDevisAsync(client, objet: "Zéta");
        await AjouterDevisAsync(client, objet: "Alpha");

        var resultat = await CreerService().RechercherAsync(new CriteresDevis
        {
            Tri = ChampTriDevis.Objet,
            TriDescendant = true,
        });

        Assert.Equal(["Zéta", "Bêta", "Alpha"], resultat.Elements.Select(devis => devis.Objet));
    }

    // SQLite traduit Contains en instr (sensible à la casse) : l'insensibilité à la casse de SQL Server
    // est vérifiée par les tests d'intégration (DevisTests).
    [Theory]
    [InlineData("Acier")]
    [InlineData("  Acier  ")]
    public async Task RechercherAsync_RechercheSurRaisonSociale_IgnoreLesEspacesEnPeripherie(string recherche)
    {
        var acier = await AjouterClientAsync("Acier SA");
        var autre = await AjouterClientAsync("Meunerie Durand");
        await AjouterDevisAsync(acier);
        await AjouterDevisAsync(autre);

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { Recherche = recherche });

        var ligne = Assert.Single(resultat.Elements);
        Assert.Equal("Acier SA", ligne.RaisonSocialeClient);
        Assert.Equal(1, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_RechercheSurNumero_RetourneLeDevisCorrespondant()
    {
        var client = await AjouterClientAsync("Acier SA");
        var cible = await AjouterDevisAsync(client, numero: "DEV-2026-00042");
        await AjouterDevisAsync(client, numero: "DEV-2026-00043");

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { Recherche = "00042" });

        Assert.Equal(cible.Id, Assert.Single(resultat.Elements).Id);
    }

    [Fact]
    public async Task RechercherAsync_RechercheSurObjetEtReference_RetourneLesDevisCorrespondants()
    {
        var client = await AjouterClientAsync("Acier SA");
        var parObjet = await AjouterDevisAsync(client, objet: "Analyse de fourrage");
        var parReference = await AjouterDevisAsync(client, reference: "fourrage-bio");
        await AjouterDevisAsync(client, objet: "Audit des sols");

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { Recherche = "fourrage" });

        Assert.Equal(
            new HashSet<int> { parObjet.Id, parReference.Id },
            resultat.Elements.Select(devis => devis.Id).ToHashSet());
    }

    [Fact]
    public async Task RechercherAsync_RechercheAvecCaractereGenerique_EstTraiteeCommeUnTexteLitteral()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterDevisAsync(client, objet: "Remise 100% bio");
        await AjouterDevisAsync(client, objet: "Audit des sols");

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { Recherche = "%" });

        Assert.Equal("Remise 100% bio", Assert.Single(resultat.Elements).Objet);
    }

    [Fact]
    public async Task RechercherAsync_RechercheSansCorrespondance_RetourneUnePageVide()
    {
        var client = await AjouterClientAsync("Acier SA");
        await AjouterDevisAsync(client);

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { Recherche = "introuvable" });

        Assert.Empty(resultat.Elements);
        Assert.Equal(0, resultat.NombreTotal);
    }

    [Fact]
    public async Task RechercherAsync_Pagination_RetourneLaPageDemandee()
    {
        var client = await AjouterClientAsync("Acier SA");
        for (var index = 0; index < 5; index++)
        {
            await AjouterDevisAsync(client, dateDevis: _dateBase.AddDays(index));
        }

        var resultat = await CreerService().RechercherAsync(new CriteresDevis { NumeroPage = 2, TaillePage = 2 });

        Assert.Equal(2, resultat.Elements.Count);
        Assert.Equal(2, resultat.NumeroPage);
        Assert.Equal(5, resultat.NombreTotal);
        Assert.Equal(3, resultat.NombrePages);
    }

    [Fact]
    public async Task RechercherAsync_TaillePageHorsBornes_EstRamenee()
    {
        var resultat = await CreerService().RechercherAsync(new CriteresDevis { TaillePage = int.MaxValue, NumeroPage = int.MaxValue });

        Assert.Equal(CriteresDevis.TaillePageMaximum, resultat.TaillePage);
        Assert.Empty(resultat.Elements);
    }

    private ServiceRechercheDevis CreerService() => new(_base.Contexte);

    private async Task<Client> AjouterClientAsync(string raisonSociale)
    {
        var client = new Client { RaisonSociale = raisonSociale };
        _base.Contexte.Clients.Add(client);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return client;
    }

    /// <summary>Le montant total est calculé avec une seule ligne taxée à 20 %.</summary>
    private async Task<Devis> AjouterDevisAsync(
        Client client,
        string? numero = null,
        string objet = "Analyse de sol",
        string? reference = null,
        StatutDevis statut = StatutDevis.Brouillon,
        DateTime? dateDevis = null,
        decimal montantHorsTaxe = 50m)
    {
        var sequence = ++_sequence;
        var devis = new Devis
        {
            Numero = numero ?? $"DEV-2026-{sequence:D5}",
            ClientId = client.Id,
            DateDevis = dateDevis ?? _dateBase,
            DateValidite = (dateDevis ?? _dateBase).AddDays(30),
            Statut = statut,
            Objet = objet,
            Reference = reference,
            Lignes =
            [
                new LigneDevis { NumeroLigne = 1, Designation = "Prestation", Quantite = 1, PrixUnitaireHorsTaxe = montantHorsTaxe, TauxTaxe = 20m },
            ],
        };
        new CalculateurDevis().Calculer(devis);

        _base.Contexte.Devis.Add(devis);
        await _base.Contexte.SaveChangesAsync();
        _base.Contexte.ChangeTracker.Clear();
        return devis;
    }
}
