using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using EmailServices.Services;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

public sealed class UserLoginEventHandler : IIntegrationEventHandler<UserLoginEvent>
{
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IEmailConfigProvider _cfgProvider;
    private readonly ILogger<UserLoginEventHandler> _log;
    private readonly IHostEnvironment _env;

    public UserLoginEventHandler(
        IEmailTemplateRenderer renderer,
        IEmailConfigProvider cfgProvider,
        ILogger<UserLoginEventHandler> log,
        IHostEnvironment env
    )
    {
        _renderer = renderer;
        _cfgProvider = cfgProvider;
        _log = log;
        _env = env;
    }

    public async Task Handle(UserLoginEvent evt)
    {
        // 1. Crear un objeto expandido con FullName calculado
        var templateData = new
        {
            evt.Id,
            evt.OccurredOn,
            evt.UserId,
            evt.Email,
            evt.Name,
            evt.LastName,
            evt.LoginTime,
            evt.IpAddress,
            evt.Device,
            evt.CompanyId,
            evt.CompanyName,
            evt.FullName,
            // Lógica para determinar FullName
            DisplayName = DetermineDisplayName(evt),
            Year = DateTime.Now.Year,
        };

        // 2. Renderizar HTML
        string tplPath = Path.Combine(_env.ContentRootPath, "Templates", "Auth", "Login.html");
        string bodyHtml = _renderer.RenderTemplate(tplPath, templateData);

        // 3. Obtener config SMTP
        var cfg = _cfgProvider.GetConfigForEvent(evt);

        // 4. Construir mensaje
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
            AlternateView avHtml = AlternateView.CreateAlternateViewFromString(
                bodyHtml,
                null,
                MediaTypeNames.Text.Html
            );
            LinkedResource logo = new(logoPath, MediaTypeNames.Image.Png)
            {
                ContentId = "logo_cid",
                TransferEncoding = TransferEncoding.Base64,
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
            EnableSsl = cfg.EnableSsl, // true
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(cfg.User, cfg.Password),
            Timeout = 15000,
        };
        try
        {
            _log.LogInformation(
                "👉 Intentando enviar correo a {0} vía {1}:{2}",
                evt.Email,
                cfg.Host,
                cfg.Port
            );
            await smtp.SendMailAsync(msg);
            _log.LogInformation("✅ Correo enviado");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo al enviar correo de login a {Email}", evt.Email);
            // Aquí podrías reintentar o guardar en base de datos para reenvío posterior
        }
    }

    private static string DetermineDisplayName(UserLoginEvent evt)
    {
        // Si tiene CompanyName y no tiene Name/LastName (usuario de oficina)
        if (
            !string.IsNullOrWhiteSpace(evt.CompanyName)
            && string.IsNullOrWhiteSpace(evt.Name)
            && string.IsNullOrWhiteSpace(evt.LastName)
        )
        {
            return evt.CompanyName;
        }

        // Si tiene Name o LastName (usuario individual)
        if (!string.IsNullOrWhiteSpace(evt.Name) || !string.IsNullOrWhiteSpace(evt.LastName))
        {
            return $"{evt.Name} {evt.LastName}".Trim();
        }

        // Fallback: usar CompanyName si existe
        if (!string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Último fallback: usar email
        return evt.Email;
    }
}
