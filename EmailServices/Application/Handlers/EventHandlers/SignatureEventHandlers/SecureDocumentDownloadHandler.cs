using System.Security.Cryptography;
using System.Text;
using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs.SignatureEvents;

namespace Handlers.EventHandlers.SignatureEventHandlers;

public sealed class SecureDocumentDownloadHandler
    : IIntegrationEventHandler<SecureDownloadSignedDocument>
{
    private readonly IEncryptionService _encryption;
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SecureDocumentDownloadHandler> _log;
    private readonly IConfiguration _cfg;

    public SecureDocumentDownloadHandler(
        IEncryptionService encryption,
        IMediator med,
        IWebHostEnvironment env,
        ILogger<SecureDocumentDownloadHandler> log,
        IConfiguration cfg
    )
    {
        _encryption = encryption;
        _med = med;
        _env = env;
        _log = log;
        _cfg = cfg;
    }

    public async Task Handle(SecureDownloadSignedDocument e)
    {
        DocumentAccessPayload payload;
        try
        {
            payload = _encryption.Decrypt<DocumentAccessPayload>(e.EncryptedPayload);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error descifrando payload sealed {Doc}", e.SealedDocumentId);
            return;
        }

        // Verificar hash
        if (!VerifyHash(payload, e.PayloadHash))
        {
            _log.LogWarning("Hash mismatch sealed doc {Doc}", e.SealedDocumentId);
            return;
        }

        if (e.ExpiresAt < DateTime.UtcNow)
        {
            _log.LogInformation("Acceso sealed expirado {Doc}", e.SealedDocumentId);
            return;
        }

        var baseUrl =
            Environment.GetEnvironmentVariable("PUBLIC_FILE_BASE_URL")
            ?? _cfg["PUBLIC_FILE_BASE_URL"]
            ?? _cfg.GetSection("Urls")["FilesBaseUrl"] // opcional si lo pones en otra sección
            ?? "https://files.mi-dominio.com";

        // Normaliza: quita trailing slash
        baseUrl = baseUrl.TrimEnd('/');

        _log.LogInformation("PUBLIC_FILE_BASE_URL (resolved) = {BaseUrl}", baseUrl);

        var downloadUrl =
            $"{baseUrl}/api/documentsigning/document?accessToken={Uri.EscapeDataString(payload.AccessToken)}&sessionId={Uri.EscapeDataString(payload.SessionId)}";

        var dto = new EmailNotificationDto(
            Template: "Signatures/SealedDocumentReady.html",
            Model: new
            {
                SignerEmail = payload.SignerEmail,
                SealedDocumentId = e.SealedDocumentId,
                DownloadUrl = downloadUrl,
                ExpiresAt = e.ExpiresAt.ToString("dd/MM/yyyy HH:mm 'UTC'"),
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Tu documento final sellado está disponible",
            To: payload.SignerEmail,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        await _med.Send(new SendEmailNotificationCommand(dto));
    }

    private bool VerifyHash(DocumentAccessPayload p, string expected)
    {
        var data = $"{p.SignerId}:{p.AccessToken}:{p.SessionId}:{p.RequestFingerprint}";
        using var sha = SHA256.Create();
        var h = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
        return string.Equals(h, expected, StringComparison.OrdinalIgnoreCase);
    }
}
