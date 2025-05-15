using System.Net;
using System.Net.Mail;
using Application.Common.DTO;
using Infrastructure.Services;
public class LEmail : IEmail
{
    private readonly ILogger<LEmail> _Log;
    private readonly IEmailSettings _settings;
    public LEmail(ILogger<LEmail> log, IEmailSettings settings)
    {

        _Log = log;
        _settings = settings;
    }
    public async Task<bool> SendEmail(EmailDTO emailDTO)
    {
        var resultConfg = await _settings.GetConfigurationByCompanyId(1);
        using var client = new SmtpClient(resultConfg.SmtpServer, resultConfg.Port)
        {
            Credentials = new NetworkCredential(resultConfg.Username, resultConfg.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(resultConfg.SenderEmail, resultConfg.SenderName),
            Subject = emailDTO.Subject,
            Body = emailDTO.Body,
            IsBodyHtml = emailDTO.IsHtml,
        };

        mailMessage.To.Add(emailDTO.To);

       await client.SendMailAsync(mailMessage);
       return Task.CompletedTask!=null? true:false;
    }
}