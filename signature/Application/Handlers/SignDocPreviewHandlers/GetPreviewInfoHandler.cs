using Application.DTOs;
using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using signature.Application.Queries;

namespace signature.Application.Handlers;

public class GetPreviewInfoHandler
    : IRequestHandler<GetPreviewInfoQuery, ApiResponse<DocumentPreviewInfoDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ILogger<GetPreviewInfoHandler> _log;

    public GetPreviewInfoHandler(SignatureDbContext db, ILogger<GetPreviewInfoHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<DocumentPreviewInfoDto>> Handle(
        GetPreviewInfoQuery request,
        CancellationToken ct
    )
    {
        // 1. Buscar preview con credenciales
        var preview = await _db
            .SignPreviewDocuments.Include(p => p.SignatureRequest)
            .Include(p => p.Signer)
            .FirstOrDefaultAsync(
                p => p.AccessToken == request.AccessToken && p.SessionId == request.SessionId,
                ct
            );

        if (preview == null)
        {
            return new(false, "Preview no encontrado o credenciales inválidas");
        }

        // 2. Verificar estado de acceso
        if (!preview.CanAccess())
        {
            return new(false, "Acceso al preview expirado o agotado");
        }

        // 3. Obtener todos los firmantes de la solicitud
        var signers = await _db
            .Signers.Where(s => s.SignatureRequestId == preview.SignatureRequestId)
            .OrderBy(s => s.Order)
            .ToListAsync(ct);

        // 4. Obtener cajas de firma para construir ubicaciones
        var signatureBoxes = await _db
            .SignatureBoxes.Where(b => signers.Select(s => s.Id).Contains(b.SignerId))
            .ToListAsync(ct);

        // 5. Construir DTOs
        var signerDtos = signers
            .Select(s => new SignerPreviewDto
            {
                SignerId = s.Id,
                SignerName = s.FullName ?? s.Email,
                SignerEmail = s.Email,
                Order = s.Order,
                Status = s.Status,
                SignedAtUtc = s.SignedAtUtc?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                IsCurrentSigner = s.Id == preview.SignerId,
            })
            .ToList();

        var signatureLocations = signatureBoxes
            .Where(b => signers.Any(s => s.Id == b.SignerId && s.Status == SignerStatus.Signed))
            .Select(b =>
            {
                var signer = signers.First(s => s.Id == b.SignerId);
                return new SignatureLocationDto
                {
                    SignerId = b.SignerId,
                    SignerName = signer.FullName ?? signer.Email,
                    SignerEmail = signer.Email,
                    Page = b.PageNumber,
                    PosX = b.PositionX,
                    PosY = b.PositionY,
                    Width = b.Width,
                    Height = b.Height,
                    SignedAtUtc = signer.SignedAtUtc?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? "",
                    SignatureType = b.Kind.ToString().ToLower(),
                    IsCurrentSigner = b.SignerId == preview.SignerId,
                };
            })
            .ToList();

        var result = new DocumentPreviewInfoDto
        {
            SealedDocumentId = preview.SealedDocumentId,
            OriginalDocumentId = preview.OriginalDocumentId,
            SignatureRequestId = preview.SignatureRequestId,
            FileName = "signed-document.pdf", // Podrías obtener esto del metadato original
            TotalPages = 0, // Se calculará en el frontend con PDF.js
            SealedAtUtc = preview.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            SignatureLocations = signatureLocations,
            Signers = signerDtos,
            PreviewAccess = new DocumentPreviewAccessDto
            {
                AccessToken = preview.AccessToken,
                SessionId = preview.SessionId,
                SealedDocumentId = preview.SealedDocumentId,
                SignerId = preview.SignerId,
                SignerEmail = preview.Signer.Email,
                ExpiresAt = preview.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                RequestFingerprint = preview.RequestFingerprint,
                CanAccess = preview.CanAccess(),
                AccessCount = preview.AccessCount,
                MaxAccessCount = preview.MaxAccessCount,
                IsActive = preview.IsActive,
                LastAccessedAt = preview.LastAccessedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            },
            Status = new DocumentPreviewStatusDto
            {
                CanPreview = preview.CanAccess(),
                IsExpired = preview.ExpiresAt <= DateTime.UtcNow,
                AccessExpired = preview.AccessCount >= preview.MaxAccessCount,
                RemainingAccess = Math.Max(0, preview.MaxAccessCount - preview.AccessCount),
                ExpiresAt = preview.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                StatusMessage = preview.CanAccess() ? "Acceso válido" : "Acceso expirado o agotado",
            },
        };

        _log.LogInformation("Preview info obtenido para firmante {SignerId}", preview.SignerId);

        return new(true, "Información de preview obtenida", result);
    }
}
