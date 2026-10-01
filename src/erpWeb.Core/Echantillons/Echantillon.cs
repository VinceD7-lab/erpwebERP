using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.Tournees;

namespace erpWeb.Core.Echantillons;

public class Echantillon : EntiteAuditable
{
    public string CodeBarresAnonyme { get; set; } = string.Empty;

    public DateTime DatePrelevement { get; set; }

    public string Filiere { get; set; } = string.Empty;

    public string StatutAnalyse { get; set; } = StatutsAnalyse.Recu;

    public decimal? TemperatureReception { get; set; }

    public int? IdTournee { get; set; }

    public TourneeRamassage? Tournee { get; set; }

    public int? IdClient { get; set; }

    public Client? Client { get; set; }

    public ResultatAgronomie? ResultatAgronomie { get; set; }
}
