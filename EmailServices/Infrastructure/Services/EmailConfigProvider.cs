using Infrastructure.Context;
using SharedLibrary.DTOs;

public record SmtpConfig(string Host, int Port, bool EnableSsl,
                        string User, string Password,
                        string FromAddress, string FromName);

public sealed class EmailConfigProvider : IEmailConfigProvider
{
  private readonly EmailContext _ctx;        // tu DbContext
  private readonly SmtpConfig _global;       // se carga desde appsettings o secrets

  public EmailConfigProvider(EmailContext ctx, IConfiguration cfg)
  {
    _ctx = ctx;
    _global = new SmtpConfig(
        cfg["Smtp:Host"]!,
        int.Parse(cfg["Smtp:Port"]!),
        bool.Parse(cfg["Smtp:EnableSsl"]!),
        cfg["Smtp:User"]!,
        cfg["Smtp:Password"]!,
        cfg["Smtp:FromAddress"]!,
        cfg["Smtp:FromName"]!
    );
  }

  public SmtpConfig GetConfigForEvent(IntegrationEvent evt)
  {
    if (evt is UserLoginEvent login)
    {
      // busca una configuración en la tabla EmailConfigs para esa compañía
      var cfg = _ctx.EmailConfigs.FirstOrDefault(c => c.CompanyId == login.CompanyId);
      if (cfg != null)
      {
        return new SmtpConfig(cfg.SmtpServer!, cfg.SmtpPort ?? 25, cfg.EnableSsl ?? true,
                              cfg.SmtpUsername!, cfg.SmtpPassword!,
                              cfg.SmtpUsername!, cfg.Name);
      }
    }
    // Fallback a config global
    return _global;
  }
}
