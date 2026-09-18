using erpWeb.Core.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace erpWeb.Infrastructure.Email;

public sealed class ServiceEmailMailKit : IServiceEmail
{
    private readonly IOptionsMonitor<OptionsSmtp> _options;
    private readonly ILogger<ServiceEmailMailKit> _journal;

    public ServiceEmailMailKit(IOptionsMonitor<OptionsSmtp> options, ILogger<ServiceEmailMailKit> journal)
    {
        _options = options;
        _journal = journal;
    }

    public async Task EnvoyerAsync(MessageEmail message, CancellationToken jetonAnnulation = default)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.Hote))
        {
            _journal.LogInformation(
                "SMTP non configuré : email « {Sujet} » destiné à {Destinataire} non envoyé.",
                message.Sujet,
                message.Destinataire);
            return;
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(options.NomExpediteur, options.AdresseExpediteur));
        email.To.Add(MailboxAddress.Parse(message.Destinataire));
        email.Subject = message.Sujet;
        email.Body = new BodyBuilder { HtmlBody = message.CorpsHtml }.ToMessageBody();

        using var client = new SmtpClient();
        var securite = options.UtiliserStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(options.Hote, options.Port, securite, jetonAnnulation);

        if (!string.IsNullOrEmpty(options.NomUtilisateur))
        {
            await client.AuthenticateAsync(options.NomUtilisateur, options.MotDePasse ?? string.Empty, jetonAnnulation);
        }

        await client.SendAsync(email, jetonAnnulation);
        await client.DisconnectAsync(true, jetonAnnulation);
    }
}
