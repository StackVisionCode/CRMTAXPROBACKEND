using Microsoft.Extensions.Logging;
using Services.Contracts;

namespace Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        // Aquí integrarías con tu servicio de correo real (SendGrid, MailKit, etc.)
        _logger.LogInformation($"Enviando email a {toEmail} con asunto: {subject}");
        // Simulamos el envío
        await Task.Delay(100);
        
        _logger.LogInformation("Email enviado exitosamente");
    }
}