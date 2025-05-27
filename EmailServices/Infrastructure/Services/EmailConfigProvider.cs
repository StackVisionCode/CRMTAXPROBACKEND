using Infrastructure.Context;
using SharedLibrary.DTOs;

public record SmtpConfig(string Host, int Port, bool EnableSsl,
                        string User, string Password,
                        string FromAddress, string FromName);

public sealed class EmailConfigProvider : IEmailConfigProvider
{
  // Esta configuracion primero verifica la configuracion en appsettings.json si falla va a la db 

  // private readonly EmailContext _ctx;        // tu DbContext
  // private readonly SmtpConfig _global;       // se carga desde appsettings o secrets

  // public EmailConfigProvider(EmailContext ctx, IConfiguration cfg)
  // {
  //   _ctx = ctx;
  //   _global = new SmtpConfig(
  //       cfg["Smtp:Host"]!,
  //       int.Parse(cfg["Smtp:Port"]!),
  //       bool.Parse(cfg["Smtp:EnableSsl"]!),
  //       cfg["Smtp:User"]!,
  //       cfg["Smtp:Password"]!,
  //       cfg["Smtp:FromAddress"]!,
  //       cfg["Smtp:FromName"]!
  //   );
  // }

  // public SmtpConfig GetConfigForEvent(IntegrationEvent evt)
  // {
  //   if (evt is UserLoginEvent login)
  //   {
  //     // busca una configuración en la tabla EmailConfigs para esa compañía
  //     var cfg = _ctx.EmailConfigs.FirstOrDefault(c => c.CompanyId == login.CompanyId);
  //     if (cfg != null)
  //     {
  //       return new SmtpConfig(cfg.SmtpServer!, cfg.SmtpPort ?? 25, cfg.EnableSsl ?? true,
  //                             cfg.SmtpUsername!, cfg.SmtpPassword!,
  //                             cfg.SmtpUsername!, cfg.Name);
  //     }
  //   }
  //   // Fallback a config global
  //   return _global;
  // }

  private readonly SmtpConfig _global;

  public EmailConfigProvider(IConfiguration cfg)
  {
    _global = new SmtpConfig(
        cfg["Smtp:Host"] ?? throw new ArgumentNullException("Smtp:Host"),
        int.Parse(cfg["Smtp:Port"] ?? "25"),
        bool.Parse(cfg["Smtp:EnableSsl"] ?? "true"),
        cfg["Smtp:User"] ?? throw new ArgumentNullException("Smtp:User"),
        cfg["Smtp:Password"] ?? throw new ArgumentNullException("Smtp:Password"),
        cfg["Smtp:FromAddress"] ?? throw new ArgumentNullException("Smtp:FromAddress"),
        cfg["Smtp:FromName"] ?? "Notificaciones"
    );
  }

  // ✔ Siempre devolvemos la config global
  public SmtpConfig GetConfigForEvent(IntegrationEvent evt) => _global;
}
