namespace erpWeb.Core.Parametres;

/// <summary>Recharge la configuration pour que IOptionsMonitor/IOptionsSnapshot reflètent les paramètres modifiés.</summary>
public interface IRechargementConfiguration
{
    void Recharger();
}
