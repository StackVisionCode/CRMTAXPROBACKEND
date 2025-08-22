using Application.DTOs;
using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using signature.Application.Queries;

namespace signature.Application.Handlers;

public class CheckAvailablePreviewHandler
    : IRequestHandler<CheckAvailablePreviewQuery, ApiResponse<AvailablePreviewDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<CheckAvailablePreviewHandler> _log;
    private readonly IConfiguration _cfg;

    public CheckAvailablePreviewHandler(
        SignatureDbContext db,
        ILogger<CheckAvailablePreviewHandler> log,
        IConfiguration cfg
    )
    {
        _db = db;
        _log = log;
        _cfg = cfg;
    }

    public async Task<ApiResponse<AvailablePreviewDto>> Handle(
        CheckAvailablePreviewQuery request,
        CancellationToken ct
    )
    {
        _log.LogInformation(
            "🔍 Verificando preview disponible para SignerId {SignerId}",
            request.SignerId
        );

        // DEBUGGING: Verificar qué hay en la base de datos para este SignerId
        var allPreviewsForSigner = await _db
            .SignPreviewDocuments.Where(p => p.SignerId == request.SignerId)
            .Select(p => new
            {
                p.Id,
                p.SignerId,
                p.AccessToken,
                p.SessionId,
                p.IsActive,
                p.ExpiresAt,
                p.AccessCount,
                p.MaxAccessCount,
                p.CreatedAt,
            })
            .ToListAsync(ct);

        _log.LogInformation(
            "📋 DEBUGGING - Total previews encontrados para SignerId {SignerId}: {Count}",
            request.SignerId,
            allPreviewsForSigner.Count
        );

        foreach (var p in allPreviewsForSigner)
        {
            _log.LogInformation(
                "📋 DEBUGGING - Preview {Id}: IsActive={IsActive}, ExpiresAt={ExpiresAt}, AccessCount={AccessCount}/{MaxAccessCount}, CreatedAt={CreatedAt}",
                p.Id,
                p.IsActive,
                p.ExpiresAt,
                p.AccessCount,
                p.MaxAccessCount,
                p.CreatedAt
            );
        }

        // DEBUGGING: Verificar información del Signer
        var signerInfo = await _db
            .Signers.Where(s => s.Id == request.SignerId)
            .Select(s => new
            {
                s.Id,
                s.CustomerId,
                s.Email,
                s.Status,
                s.SignedAtUtc,
            })
            .FirstOrDefaultAsync(ct);

        if (signerInfo != null)
        {
            _log.LogInformation(
                "📋 DEBUGGING - Signer encontrado: Id={SignerId}, CustomerId={CustomerId}, Email={Email}, Status={Status}, SignedAt={SignedAt}",
                signerInfo.Id,
                signerInfo.CustomerId,
                signerInfo.Email,
                signerInfo.Status,
                signerInfo.SignedAtUtc
            );
        }
        else
        {
            _log.LogWarning(
                "⚠️ DEBUGGING - No se encontró Signer con Id {SignerId}",
                request.SignerId
            );
        }

        // Buscar preview activo para este firmante
        var preview = await _db
            .SignPreviewDocuments.Where(p =>
                p.SignerId == request.SignerId
                && p.IsActive
                && p.ExpiresAt > DateTime.UtcNow
                && p.AccessCount < p.MaxAccessCount
            )
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (preview == null)
        {
            _log.LogInformation(
                "❌ No hay preview disponible para SignerId {SignerId}",
                request.SignerId
            );

            // DEBUGGING: Verificar por qué no hay preview disponible
            var anyPreview = await _db
                .SignPreviewDocuments.Where(p => p.SignerId == request.SignerId)
                .FirstOrDefaultAsync(ct);

            if (anyPreview == null)
            {
                _log.LogWarning(
                    "⚠️ DEBUGGING - No existe NINGÚN preview para SignerId {SignerId}. "
                        + "Posibles causas: "
                        + "1) El documento aún no está sellado, "
                        + "2) El evento SecureDownloadSignedDocument no se ha procesado, "
                        + "3) Error en el SignerId proporcionado",
                    request.SignerId
                );
            }
            else
            {
                _log.LogWarning(
                    "⚠️ DEBUGGING - Existe preview pero no cumple condiciones: "
                        + "IsActive={IsActive}, ExpiresAt={ExpiresAt} (now={Now}), "
                        + "AccessCount={AccessCount}/{MaxAccessCount}",
                    anyPreview.IsActive,
                    anyPreview.ExpiresAt,
                    DateTime.UtcNow,
                    anyPreview.AccessCount,
                    anyPreview.MaxAccessCount
                );
            }

            return new(
                true,
                "Preview aún no disponible",
                new AvailablePreviewDto
                {
                    HasPreview = false,
                    Message =
                        "El documento final se está procesando. Recibirás un enlace por correo cuando esté listo.",
                }
            );
        }

        // DEBUGGING: Log del preview encontrado
        _log.LogInformation(
            "DEBUGGING - Preview encontrado: "
                + "Id={Id}, SignerId={SignerId}, AccessToken={AccessToken}, SessionId={SessionId}, "
                + "IsActive={IsActive}, ExpiresAt={ExpiresAt}, AccessCount={AccessCount}",
            preview.Id,
            preview.SignerId,
            preview.AccessToken.Substring(0, 8) + "...",
            preview.SessionId.Substring(0, 8) + "...",
            preview.IsActive,
            preview.ExpiresAt,
            preview.AccessCount
        );

        // Construir URL de preview
        var baseUrl = _cfg["Frontend:BaseUrl"] ?? "https://signature.mi-dominio.com";
        baseUrl = baseUrl.TrimEnd('/');
        var previewUrl =
            $"{baseUrl}/signature/preview?accessToken={Uri.EscapeDataString(preview.AccessToken)}&sessionId={Uri.EscapeDataString(preview.SessionId)}";

        _log.LogInformation(
            "Preview disponible para SignerId {SignerId} - URL generada: {PreviewUrl}",
            request.SignerId,
            previewUrl
        );

        return new(
            true,
            "Preview disponible",
            new AvailablePreviewDto
            {
                HasPreview = true,
                PreviewUrl = previewUrl,
                AccessToken = preview.AccessToken,
                SessionId = preview.SessionId,
                ExpiresAt = preview.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Message = "El documento final está listo para visualizar.",
            }
        );
    }
}
