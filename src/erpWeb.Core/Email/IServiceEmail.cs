namespace erpWeb.Core.Email;

public interface IServiceEmail
{
    Task EnvoyerAsync(MessageEmail message, CancellationToken jetonAnnulation = default);
}
