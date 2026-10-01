using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;

namespace erpWeb.Core.Tournees;

public class TourneeRamassage : EntiteAuditable
{
    public DateOnly DateTournee { get; set; }

    public string NomChauffeur { get; set; } = string.Empty;

    public string? ImmatriculationCamion { get; set; }

    public string? StatutTemperature { get; set; }

    public ICollection<Echantillon> Echantillons { get; set; } = [];
}
