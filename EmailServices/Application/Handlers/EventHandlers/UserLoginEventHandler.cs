using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using SharedLibrary.DTOs;
using SharedLibrary.Contracts;
using EmailServices.Services;

public sealed class UserLoginEventHandler : IIntegrationEventHandler<UserLoginEvent>
{
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IEmailConfigProvider _cfgProvider;
    private readonly ILogger<UserLoginEventHandler> _log;
    private readonly IHostEnvironment _env;

    public UserLoginEventHandler(IEmailTemplateRenderer renderer,
                                IEmailConfigProvider cfgProvider,
                                ILogger<UserLoginEventHandler> log,
                                IHostEnvironment env)
    {
        _renderer = renderer;
        _cfgProvider = cfgProvider;
        _log = log;
        _env = env;
    }

    public async Task Handle(UserLoginEvent evt)
    {
        // 1. Renderizar HTML
        string tplPath = Path.Combine(_env.ContentRootPath, "Templates", "Auth", "Login.html");
        string bodyHtml = _renderer.RenderTemplate(tplPath, evt);

        // 2. Obtener config SMTP
        var cfg = _cfgProvider.GetConfigForEvent(evt);

        // 3. Construir mensaje
        var msg = new MailMessage
        {
            From = new MailAddress(cfg.FromAddress, cfg.FromName),
            Subject = "Nuevo inicio de sesión detectado",
            IsBodyHtml = true,
        };
        msg.To.Add(evt.Email);

        // ---- Adjuntar imagen embebida (logo) ----------
        string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        string logoPath = Path.Combine(assetsDir, "logo.png");
        if (File.Exists(logoPath))
        {
            AlternateView avHtml = AlternateView.CreateAlternateViewFromString(bodyHtml, null, MediaTypeNames.Text.Html);
            LinkedResource logo = new(logoPath, MediaTypeNames.Image.Png)
            {
                ContentId = "logo_cid",
                TransferEncoding = TransferEncoding.Base64
            };
            avHtml.LinkedResources.Add(logo);
            msg.AlternateViews.Add(avHtml);
        }
        else
        {
            msg.Body = bodyHtml; // sin logo
        }

        // 4. Enviar
        using var smtp = new SmtpClient(cfg.Host, cfg.Port)
        {
            EnableSsl = cfg.EnableSsl,   // true
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(cfg.User, cfg.Password),
            Timeout = 15000
        };
        try
        {
            _log.LogInformation("👉 Intentando enviar correo a {0} vía {1}:{2}", evt.Email, cfg.Host, cfg.Port);
            await smtp.SendMailAsync(msg);
            _log.LogInformation("✅ Correo enviado");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo al enviar correo de login a {Email}", evt.Email);
            // Aquí podrías reintentar o guardar en base de datos para reenvío posterior
        }
    }
}
