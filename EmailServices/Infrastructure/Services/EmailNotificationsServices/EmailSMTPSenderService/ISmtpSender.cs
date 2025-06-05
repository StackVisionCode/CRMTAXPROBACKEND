using System.Net.Mail;

namespace EmailServices.Services.EmailNotificationsServices;

public interface ISmtpSender
{
    Task SendAsync(MailMessage message, SmtpConfig cfg, CancellationToken ct);
}
