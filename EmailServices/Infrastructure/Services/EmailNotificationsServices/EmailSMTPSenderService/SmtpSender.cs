using System.Net.Mail;

namespace EmailServices.Services.EmailNotificationsServices;

public sealed class SmtpSender : ISmtpSender
{
    private readonly ILogger<SmtpSender> _log;

    public SmtpSender(ILogger<SmtpSender> log) => _log = log;

    public async Task SendAsync(MailMessage msg, SmtpConfig cfg, CancellationToken ct)
    {
        try
        {
            using var smtp = new SmtpClient(cfg.Host, cfg.Port)
            {
                EnableSsl = cfg.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(cfg.User, cfg.Password),
            };

            _log.LogInformation("Sending email to {0} via {1}:{2}", msg.To, cfg.Host, cfg.Port);
            await smtp.SendMailAsync(msg, ct);
            _log.LogInformation("✅ Correo enviado");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo al enviar correo de login.");
        }
    }
}
