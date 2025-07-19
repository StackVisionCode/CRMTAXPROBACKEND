using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using signature.Infrastruture.Commands;

namespace signature.Application.Handlers;

public class RegisterConsentHandler : IRequestHandler<RegisterConsentCommand, ApiResponse<bool>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<RegisterConsentHandler> _log;

    // (Opcional) private readonly IEventBus _bus;

    public RegisterConsentHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<RegisterConsentHandler> log
    // , IEventBus bus
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
        // _bus = bus;
    }

    public async Task<ApiResponse<bool>> Handle(
        RegisterConsentCommand command,
        CancellationToken ct
    )
    {
        try
        {
            var payload = command.Payload;

            // 1. Validar token (scope "sign")
            var (ok, signerId, requestId) = _tokenSvc.Validate(payload.Token, "sign");
            if (!ok)
                return new(false, "Token inválido o expirado");

            // 2. Cargar signer y estado de la request (sin traer todas las cajas)
            var signer = await _db.Signers.FirstOrDefaultAsync(
                s => s.Id == signerId && s.SignatureRequestId == requestId,
                ct
            );

            if (signer is null)
                return new(false, "Firmante no encontrado");

            // 3. (Opcional) validar estado global de la request
            var request = await _db
                .SignatureRequests.Where(r => r.Id == requestId)
                .Select(r => new { r.Status })
                .FirstOrDefaultAsync(ct);

            if (request is null)
                return new(false, "Solicitud no encontrada");

            if (
                request.Status
                is SignatureStatus.Rejected
                    or SignatureStatus.Canceled
                    or SignatureStatus.Completed
            )
                return new(false, "Esta solicitud no acepta consentimientos (estado final).");

            if (signer.Status == SignerStatus.Signed)
                return new(false, "Este firmante ya firmó; no requiere registrar consentimiento.");

            // 4. Registrar consentimiento (idempotente)
            bool yaExistia = signer.ConsentAgreedAtUtc.HasValue;

            signer.RegisterConsent(
                payload.AgreedAtUtc,
                payload.ConsentText,
                payload.ButtonText,
                payload.ClientIp,
                payload.UserAgent
            );

            if (yaExistia)
            {
                _log.LogInformation("Consentimiento ya existía para signer {SignerId}", signerId);
                return new(true, "Consentimiento ya estaba registrado");
            }

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Consentimiento registrado para signer {SignerId} en request {RequestId}",
                signerId,
                requestId
            );

            // (Opcional) publicar evento para auditoría
            // _bus.Publish(new ConsentRegisteredEvent(...));

            return new(true, "Consentimiento registrado");
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(false, "Conflicto de concurrencia, intenta de nuevo.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error registrando consentimiento");
            return new(false, "Error interno");
        }
    }
}
