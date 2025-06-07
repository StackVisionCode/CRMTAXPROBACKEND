using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Application.Common.DTO;
using EmailServices.Services;
using EmailServices.Services.EmailNotificationsServices;

public sealed class EmailBuilder : IEmailBuilder
{
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IWebHostEnvironment _env;

    public EmailBuilder(IEmailTemplateRenderer renderer, IWebHostEnvironment env)
    {
        _renderer = renderer;
        _env = env;
    }

    public MailMessage Build(EmailNotificationDto dto, SmtpConfig cfg)
    {
        string tplPath = Path.Combine(_env.ContentRootPath, "Templates", dto.Template);
        string bodyHtml = _renderer.RenderTemplate(tplPath, dto.Model);

        var msg = new MailMessage
        {
            From = new MailAddress(cfg.FromAddress, cfg.FromName),
            Subject = dto.Subject,
            IsBodyHtml = true,
        };
        msg.To.Add(dto.To);

        // Inline logo si viene ruta
        if (!string.IsNullOrWhiteSpace(dto.InlineLogoPath) && File.Exists(dto.InlineLogoPath))
        {
            AlternateView av = AlternateView.CreateAlternateViewFromString(
                bodyHtml,
                null,
                MediaTypeNames.Text.Html
            );
            var logo = new LinkedResource(
                dto.InlineLogoPath,
                new ContentType(MediaTypeNames.Image.Png)
            )
            {
                ContentId = "logo_cid",
                TransferEncoding = TransferEncoding.Base64,
            };
            av.LinkedResources.Add(logo);
            msg.AlternateViews.Add(av);
        }
        else
        {
            msg.Body = bodyHtml;
        }
        return msg;
    }
}
