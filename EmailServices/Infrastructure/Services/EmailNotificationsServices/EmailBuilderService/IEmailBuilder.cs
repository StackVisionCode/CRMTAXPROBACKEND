using System.Net.Mail;
using Application.Common.DTO;

namespace EmailServices.Services.EmailNotificationsServices;

public interface IEmailBuilder
{
    MailMessage Build(EmailNotificationDto dto, SmtpConfig cfg);
}
