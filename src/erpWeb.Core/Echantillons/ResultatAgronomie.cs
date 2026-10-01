using erpWeb.Core.Communs;

namespace erpWeb.Core.Echantillons;

public class ResultatAgronomie : EntiteAuditable
{
    public int IdEchantillon { get; set; }

    public Echantillon? Echantillon { get; set; }

    public string? TypeSupport { get; set; }

    public decimal? PhSol { get; set; }

    public decimal? MatiereOrganique { get; set; }

    public decimal? PhosphoreP2O5 { get; set; }

    public decimal? PotassiumK2O { get; set; }

    public decimal? ReliquatAzoteN { get; set; }

    public decimal? ValeurUclFourrage { get; set; }

    public DateTime? DateValidation { get; set; }
}
