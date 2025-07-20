using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;
using signature.Application.DTOs;
using signature.Infrastruture.Commands;

public class RejectSignatureHandler
    : IRequestHandler<RejectSignatureCommand, ApiResponse<RejectResultDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<RejectSignatureHandler> _log;
    private readonly IEventBus _bus; // si quieres notificar

    public RejectSignatureHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<RejectSignatureHandler> log,
        IEventBus bus
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
        _bus = bus;
    }

    public async Task<ApiResponse<RejectResultDto>> Handle(
        RejectSignatureCommand command,
        CancellationToken ct
    )
    {
        try
        {
            // 1. Validar token (igual que Submit)
            var (ok, signerId, reqId) = _tokenSvc.Validate(command.Payload.Token, "sign");
            if (!ok)
                return new(false, "Token inválido o expirado");

            // 2. Cargar request + signers
            var req = await _db
                .SignatureRequests.Include(r => r.Signers)
                .FirstOrDefaultAsync(r => r.Id == reqId, ct);

            if (req is null)
                return new(false, "Solicitud no encontrada");

            // 3. Estados terminales que impiden rechazo
            if (
                req.Status
                is SignatureStatus.Completed
                    or SignatureStatus.Canceled
                    or SignatureStatus.Expired
            )
                return new(false, $"La solicitud ya está {req.Status} y no puede ser rechazada.");

            if (req.Status == SignatureStatus.Rejected)
            {
                var alreadySigner = req.Signers.FirstOrDefault(s => s.Id == signerId);
                return ApiResponse<RejectResultDto>.Ok(
                    new RejectResultDto
                    {
                        SignatureRequestId = req.Id,
                        SignerId = signerId,
                        RequestStatus = req.Status,
                        SignerStatus = alreadySigner?.Status ?? SignerStatus.Rejected,
                        RejectedAtUtc = req.RejectedAtUtc ?? DateTime.UtcNow,
                        Reason = req.RejectReason,
                    },
                    "Ya estaba rechazada (idempotente)."
                );
            }

            // 4. Encontrar signer
            var signer = req.Signers.FirstOrDefault(x => x.Id == signerId);
            if (signer is null)
                return new(false, "Firmante no encontrado");

            if (signer.Status == SignerStatus.Signed)
                return new(false, "No se puede rechazar: el firmante ya firmó.");

            // 5. Marcar entidades
            signer.MarkRejected(command.Payload.Reason);
            req.MarkRejected(signerId, command.Payload.Reason);

            await _db.SaveChangesAsync(ct);

            var rejectedAt = signer.RejectedAtUtc!.Value;

            // 6. Emitir evento de rechazo (RabbitMQ)
            foreach (var dest in req.Signers)
            {
                _bus.Publish(
                    new SignatureRequestRejectedEvent(
                        Id: Guid.NewGuid(),
                        OccurredOn: DateTime.UtcNow,
                        SignatureRequestId: req.Id,
                        DocumentId: req.DocumentId,
                        RejectedBySignerId: signer.Id,
                        RejectedByEmail: signer.Email,
                        RejectedByFullName: signer.FullName,
                        RecipientSignerId: dest.Id,
                        RecipientEmail: dest.Email,
                        RecipientFullName: dest.FullName,
                        Reason: signer.RejectReason,
                        RejectedAtUtc: rejectedAt
                    )
                );
            }

            _log.LogInformation("Solicitud {Req} rechazada por signer {Signer}", req.Id, signer.Id);

            return ApiResponse<RejectResultDto>.Ok(
                new RejectResultDto
                {
                    SignatureRequestId = req.Id,
                    SignerId = signer.Id,
                    RequestStatus = req.Status,
                    SignerStatus = signer.Status,
                    RejectedAtUtc = signer.RejectedAtUtc!.Value,
                    Reason = signer.RejectReason,
                },
                "Solicitud rechazada"
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al rechazar la solicitud");
            return new(false, "Error interno del servidor");
        }
    }
}
