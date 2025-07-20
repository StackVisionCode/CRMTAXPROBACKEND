using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Application.Handlers;

public class CheckSignerStatusHandler
    : IRequestHandler<CheckSignerStatusQuery, ApiResponse<SignerStatusDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<CheckSignerStatusHandler> _log;

    public CheckSignerStatusHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<CheckSignerStatusHandler> log
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
    }

    public async Task<ApiResponse<SignerStatusDto>> Handle(
        CheckSignerStatusQuery request,
        CancellationToken ct
    )
    {
        // 1. Validar token de forma segura
        var (isValid, signerId, requestId) = _tokenSvc.Validate(request.Token, "sign");
        if (!isValid)
        {
            _log.LogWarning("Token inválido recibido en CheckSignerStatus");
            return new(false, "Token inválido o expirado");
        }

        _log.LogInformation(
            "Consultando estado del firmante {SignerId} en solicitud {RequestId}",
            signerId,
            requestId
        );

        // 2. Obtener información completa del firmante y la solicitud con JOINs seguros
        var signerData = await (
            from signer in _db.Signers
            join signatureRequest in _db.SignatureRequests
                on signer.SignatureRequestId equals signatureRequest.Id
            where signer.Id == signerId && signatureRequest.Id == requestId
            select new
            {
                // Datos del firmante
                SignerId = signer.Id,
                Email = signer.Email,
                Status = signer.Status,
                SignedAtUtc = signer.SignedAtUtc,
                RejectedAtUtc = signer.RejectedAtUtc,
                RejectReason = signer.RejectReason,
                FullName = signer.FullName,
                Order = signer.Order,

                // Datos de la solicitud
                RequestStatus = signatureRequest.Status,
                RequestId = signatureRequest.Id,
            }
        ).AsNoTracking().FirstOrDefaultAsync(ct);

        if (signerData == null)
        {
            _log.LogWarning(
                "Firmante {SignerId} no encontrado en solicitud {RequestId}",
                signerId,
                requestId
            );
            return new(false, "Firmante no encontrado");
        }

        // 3. Obtener estadísticas de la solicitud (total y completados)
        var requestStats = await (
            from signer in _db.Signers
            where signer.SignatureRequestId == requestId
            group signer by signer.SignatureRequestId into g
            select new
            {
                TotalSigners = g.Count(),
                CompletedSigners = g.Count(s =>
                    s.Status == SignerStatus.Signed || s.Status == SignerStatus.Rejected
                ),
            }
        )
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // 4. Determinar si el proceso está completado para este firmante
        bool isProcessCompleted =
            signerData.Status == SignerStatus.Signed
            || signerData.Status == SignerStatus.Rejected
            || signerData.RequestStatus == SignatureStatus.Rejected
            || signerData.RequestStatus == SignatureStatus.Completed;

        // 5. Determinar si puede proceder (continuar con el flujo normal)
        bool canProceed =
            !isProcessCompleted && signerData.RequestStatus == SignatureStatus.Pending;

        // 6. Generar mensaje descriptivo
        string statusMessage = GenerateStatusMessage(
            signerData.Status,
            signerData.RequestStatus,
            signerData.FullName
        );

        // 7. Construir DTO de respuesta
        var dto = new SignerStatusDto
        {
            SignerId = signerData.SignerId,
            Email = signerData.Email,
            Status = signerData.Status,
            RequestStatus = signerData.RequestStatus,
            IsProcessCompleted = isProcessCompleted,
            SignedAtUtc = signerData.SignedAtUtc,
            RejectedAtUtc = signerData.RejectedAtUtc,
            RejectReason = signerData.RejectReason,
            FullName = signerData.FullName,
            Order = signerData.Order,
            TotalSigners = requestStats?.TotalSigners ?? 1,
            CompletedSigners = requestStats?.CompletedSigners ?? 0,
            CanProceed = canProceed,
            StatusMessage = statusMessage,
        };

        _log.LogInformation(
            "Estado consultado: Firmante {SignerId}, Status: {Status}, CanProceed: {CanProceed}",
            signerId,
            signerData.Status,
            canProceed
        );

        return new(true, "Estado obtenido exitosamente", dto);
    }

    /// <summary>
    /// Genera un mensaje descriptivo basado en el estado
    /// </summary>
    private static string GenerateStatusMessage(
        SignerStatus signerStatus,
        SignatureStatus requestStatus,
        string? fullName
    )
    {
        var name = !string.IsNullOrEmpty(fullName) ? fullName : "Firmante";

        return signerStatus switch
        {
            SignerStatus.Signed => $"¡Perfecto! {name} ya ha firmado este documento exitosamente.",
            SignerStatus.Rejected => $"{name} ha rechazado la firma de este documento.",
            SignerStatus.Pending when requestStatus == SignatureStatus.Rejected =>
                "Esta solicitud de firma ha sido rechazada y ya no está disponible.",
            SignerStatus.Pending when requestStatus == SignatureStatus.Completed =>
                "Este documento ya ha sido completamente firmado por todos los firmantes.",
            SignerStatus.Pending when requestStatus == SignatureStatus.Expired =>
                "Esta solicitud de firma ha expirado.",
            SignerStatus.Pending when requestStatus == SignatureStatus.Canceled =>
                "Esta solicitud de firma ha sido cancelada.",
            SignerStatus.Pending => $"{name}, puedes proceder a revisar y firmar el documento.",
            _ => "Estado del documento no reconocido.",
        };
    }
}
