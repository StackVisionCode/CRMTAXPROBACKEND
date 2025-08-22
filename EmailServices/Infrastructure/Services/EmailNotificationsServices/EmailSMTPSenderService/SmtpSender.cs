using System.Net;
using System.Net.Mail;
using Polly; // <-- Paquete NuGet Polly
using Polly.Retry;

namespace EmailServices.Services.EmailNotificationsServices;

public sealed class SmtpSender : ISmtpSender
{
    private readonly ILogger<SmtpSender> _log;

    // ❶ 1-solo envío concurrente ⇒ evitamos “server busy”
    private static readonly SemaphoreSlim _throttle = new(1, 1);

    // ❷ Retry con back-off exponencial (Polly)
    private static readonly AsyncRetryPolicy _retry = Policy
        .Handle<SmtpException>(ex =>
            ex.StatusCode == SmtpStatusCode.ServiceNotAvailable
            || (int)ex.StatusCode is >= 400 and < 500
        )
        .WaitAndRetryAsync( // 3 intentos: 1 s, 4 s, 9 s
            3,
            i => TimeSpan.FromSeconds(i * i),
            (ex, ts, n, ctx) =>
            {
                // logging del intento
                var log = (ILogger<SmtpSender>)ctx["logger"]!;
                log.LogWarning(
                    ex,
                    "SMTP intento {Attempt} ha fallado, reintentar en {Delay}s…",
                    n,
                    ts.TotalSeconds
                );
            }
        );

    public SmtpSender(ILogger<SmtpSender> log) => _log = log;

    public async Task SendAsync(MailMessage msg, SmtpConfig cfg, CancellationToken ct)
    {
        await _throttle.WaitAsync(ct); // ❸ bloquea envíos simultáneos
        try
        {
            await _retry.ExecuteAsync(
                async ctx =>
                {
                    using var smtp = new SmtpClient(cfg.Host, cfg.Port)
                    {
                        EnableSsl = cfg.EnableSsl,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(cfg.User, cfg.Password),
                    };

                    _log.LogInformation(
                        "Enviando e-mail a {To} vía {Host}:{Port}",
                        msg.To,
                        cfg.Host,
                        cfg.Port
                    );

                    await smtp.SendMailAsync(msg, ct);

                    _log.LogInformation("Correo enviado a {To}", msg.To);
                },
                new Context { ["logger"] = _log }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ No se pudo enviar el correo a {To}", msg.To);
            throw; // deja que el handler decida qué hacer (Dead-letter, etc.)
        }
        finally
        {
            _throttle.Release();
        }
    }
}
