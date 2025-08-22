using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Application.Handlers;

public class GetSigningLayoutHandler
    : IRequestHandler<GetSigningLayoutQuery, ApiResponse<SigningLayoutDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<GetSigningLayoutHandler> _log;

    public GetSigningLayoutHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<GetSigningLayoutHandler> log
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
    }

    public async Task<ApiResponse<SigningLayoutDto>> Handle(
        GetSigningLayoutQuery request,
        CancellationToken ct
    )
    {
        // 1. Validar token (scope "sign")
        var (ok, signerId, requestId) = _tokenSvc.Validate(request.Token, "sign");
        if (!ok)
            return new(false, "Token inválido o expirado");

        _log.LogInformation(
            "🔍 Buscando layout para SignerId: {SignerId}, RequestId: {RequestId}",
            signerId,
            requestId
        );

        // 2. Obtener estado básico de la solicitud
        var reqRow = await _db
            .SignatureRequests.Where(r => r.Id == requestId)
            .Select(r => new
            {
                r.Id,
                r.DocumentId,
                r.Status,
            })
            .FirstOrDefaultAsync(ct);

        if (reqRow is null)
            return new(false, "Solicitud no encontrada");

        if (reqRow.Status == SignatureStatus.Rejected)
            return new(false, "La solicitud fue rechazada.");

        // 3. Verificar que el firmante existe y pertenece a la solicitud
        var signerInfo = await _db
            .Signers.Where(s => s.Id == signerId && s.SignatureRequestId == requestId)
            .Select(s => new
            {
                s.Id,
                s.Order,
                s.Status,
                s.SignedAtUtc,
                s.FullName,
                s.SignatureRequestId,
            })
            .FirstOrDefaultAsync(ct);

        if (signerInfo is null)
        {
            _log.LogWarning(
                "❌ Firmante {SignerId} no encontrado en la solicitud {RequestId}",
                signerId,
                requestId
            );
            return new(false, "Firmante no encontrado");
        }

        _log.LogInformation(
            "Firmante encontrado: {SignerId}, Order: {Order}, Status: {Status}",
            signerInfo.Id,
            signerInfo.Order,
            signerInfo.Status
        );

        // 4. Debug: Verificar cuántas cajas existen para este firmante
        var boxCount = await _db.SignatureBoxes.Where(b => b.SignerId == signerId).CountAsync(ct);

        _log.LogInformation(
            "📦 Total de cajas para el firmante {SignerId}: {BoxCount}",
            signerId,
            boxCount
        );

        if (boxCount == 0)
        {
            // Debug adicional: verificar si existen cajas en general
            var totalBoxes = await _db.SignatureBoxes.CountAsync(ct);
            var boxesInRequest = await _db
                .SignatureBoxes.Where(b =>
                    _db.Signers.Any(s => s.Id == b.SignerId && s.SignatureRequestId == requestId)
                )
                .CountAsync(ct);

            _log.LogWarning(
                "⚠️ No hay cajas para el firmante {SignerId}. Total cajas en DB: {Total}, Cajas en esta solicitud: {InRequest}",
                signerId,
                totalBoxes,
                boxesInRequest
            );

            return new(
                false,
                "No se encontraron posiciones de firma configuradas para este firmante"
            );
        }

        // 5. Obtener las cajas del firmante con toda la información
        var signerBoxes = await _db
            .SignatureBoxes.Where(b => b.SignerId == signerId)
            .OrderBy(b => b.PageNumber)
            .ThenBy(b => b.PositionY)
            .Select(b => new
            {
                BoxId = b.Id,
                PageNumber = b.PageNumber,
                PositionX = b.PositionX,
                PositionY = b.PositionY,
                Width = b.Width,
                Height = b.Height,
                Kind = b.Kind,
                InitialValue = b.InitialEntity != null ? b.InitialEntity.InitalValue : null,
                DateValue = b.FechaSigner != null ? b.FechaSigner.FechaValue : null,
            })
            .AsNoTracking()
            .ToListAsync(ct);

        _log.LogInformation(
            "📋 Cajas obtenidas para el firmante {SignerId}: {BoxCount}",
            signerId,
            signerBoxes.Count
        );

        // 6. Mapear a DTOs
        var boxDtos = signerBoxes
            .Select(b => new SigningBoxDto
            {
                BoxId = b.BoxId,
                Page = b.PageNumber,
                PosX = b.PositionX,
                PosY = b.PositionY,
                Width = b.Width,
                Height = b.Height,
                Kind = b.Kind,
                SignerId = signerInfo.Id,
                SignerOrder = signerInfo.Order,
                SignerStatus = signerInfo.Status,
                IsCurrentSigner = true, // Siempre true porque solo devolvemos cajas del firmante actual
                SignedAtUtc = signerInfo.SignedAtUtc,
                InitialsValue = b.InitialValue,
                DateValue = b.DateValue,
            })
            .ToList();

        var dto = new SigningLayoutDto
        {
            SignatureRequestId = reqRow.Id,
            DocumentId = reqRow.DocumentId,
            RequestStatus = reqRow.Status,
            CurrentSignerId = signerInfo.Id,
            CurrentSignerOrder = signerInfo.Order,
            Boxes = boxDtos,
        };

        _log.LogInformation(
            "Layout devuelto para Request {RequestId} - Signer {SignerId} con {BoxCount} cajas",
            reqRow.Id,
            signerInfo.Id,
            dto.Boxes.Count
        );

        return new(true, "Layout obtenido", dto);
    }
}
