using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;
using signature.Infrastruture.Commands;
using SixLabors.ImageSharp;

namespace signature.Application.Handlers
{
    public class SubmitSignatureHandler : IRequestHandler<SubmitSignatureCommand, ApiResponse<bool>>
    {
        private readonly SignatureDbContext _db;
        private readonly ISignatureValidToken _tokenSvc;
        private readonly IEventBus _bus;
        private readonly ILogger<SubmitSignatureHandler> _log;

        public SubmitSignatureHandler(
            SignatureDbContext db,
            ISignatureValidToken tokenSvc,
            IEventBus bus,
            ILogger<SubmitSignatureHandler> log
        )
        {
            _db = db;
            _tokenSvc = tokenSvc;
            _bus = bus;
            _log = log;
        }

        public async Task<ApiResponse<bool>> Handle(
            SubmitSignatureCommand command,
            CancellationToken cancellationToken
        )
        {
            try
            {
                // 1. Validar token
                var (isValid, signerId, reqId) = _tokenSvc.Validate(command.Payload.Token, "sign");
                if (!isValid)
                    return new(false, "Token inválido o expirado");

                // 2. Obtener la solicitud y el firmante con una consulta simple
                var requestData = await (
                    from request in _db.SignatureRequests
                    join signerEntity in _db.Signers
                        on request.Id equals signerEntity.SignatureRequestId
                    where request.Id == reqId && signerEntity.Id == signerId
                    select new { Request = request, Signer = signerEntity }
                ).FirstOrDefaultAsync(cancellationToken);

                if (requestData == null)
                    return new ApiResponse<bool>(
                        false,
                        "Solicitud de firma o firmante no encontrado"
                    );

                var req = requestData.Request;
                var signer = requestData.Signer;

                if (signer.ConsentAgreedAtUtc is null)
                    return new(false, "Debe registrar el consentimiento antes de firmar.");

                if (req.Status == SignatureStatus.Rejected)
                    return new(false, "El proceso fue rechazado y no acepta firmas.");

                if (signer.Status == SignerStatus.Signed)
                    return new ApiResponse<bool>(false, "Esta firma ya ha sido registrada");

                // 3. Validar certificado
                if (command.Payload.Certificate.NotAfter < DateTime.UtcNow)
                    return new ApiResponse<bool>(false, "El certificado digital ha expirado");

                if (!IsValidPngBase64(command.Payload.SignatureImageBase64, out var cleanB64))
                    return new(false, "La imagen de la firma está corrupta o incompleta.");

                command.Payload.SignatureImageBase64 = cleanB64; // normalizado

                // 4. Crear certificado digital
                var cert = new DigitalCertificate(
                    command.Payload.Certificate.Thumbprint,
                    command.Payload.Certificate.Subject,
                    command.Payload.Certificate.NotBefore,
                    command.Payload.Certificate.NotAfter
                );

                // 5. Marcar el signer como firmado directamente (sin usar req.ReceiveSignature)
                signer.MarkSigned(
                    cleanB64,
                    cert,
                    command.Payload.SignedAtUtc,
                    command.Payload.ClientIp,
                    command.Payload.UserAgent,
                    command.Payload.ConsentAgreedAtUtc,
                    command.Payload.Consent_text,
                    command.Payload.Consent_button_text
                );

                // 6. Actualizar las cajas del firmante
                var signerBoxes = await _db
                    .SignatureBoxes.Where(b => b.SignerId == signerId)
                    .ToListAsync(cancellationToken);

                foreach (var box in signerBoxes)
                    box.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                // 7. Verificar si quedan firmantes pendientes
                bool hasPending = await _db
                    .Signers.Where(s =>
                        s.SignatureRequestId == reqId && s.Status == SignerStatus.Pending
                    )
                    .AnyAsync(cancellationToken);

                if (!hasPending)
                {
                    // Obtener todos los datos para el evento "documento completo"
                    var allSignedData = await (
                        from signerData in _db.Signers
                        join boxData in _db.SignatureBoxes on signerData.Id equals boxData.SignerId
                        where
                            signerData.SignatureRequestId == reqId
                            && signerData.Status == SignerStatus.Signed
                        select new { Signer = signerData, Box = boxData }
                    ).ToListAsync(cancellationToken);

                    var signedImages = allSignedData
                        .Select(data => new SignedImageDto(
                            data.Signer.CustomerId ?? Guid.Empty,
                            data.Signer.Id,
                            data.Signer.Email!,
                            data.Box.PageNumber,
                            data.Box.PositionX,
                            data.Box.PositionY,
                            data.Box.Width,
                            data.Box.Height,
                            (data.Box.InitialEntity is null && data.Box.FechaSigner is null)
                                ? data.Signer.SignatureImage
                                : null,
                            data.Signer.Certificate!.Thumbprint,
                            data.Signer.SignedAtUtc!.Value,
                            data.Signer.ClientIp ?? string.Empty,
                            data.Signer.UserAgent ?? string.Empty,
                            data.Signer.ConsentAgreedAtUtc!.Value,
                            data.Box.InitialEntity is null
                                ? null
                                : new InitialStampDto(
                                    data.Box.InitialEntity.InitalValue,
                                    data.Box.InitialEntity.PositionXIntial,
                                    data.Box.InitialEntity.PositionYIntial,
                                    data.Box.InitialEntity.WidthIntial,
                                    data.Box.InitialEntity.HeightIntial
                                ),
                            data.Box.FechaSigner is null
                                ? null
                                : new DateStampDto(
                                    data.Box.FechaSigner.FechaValue,
                                    data.Box.FechaSigner.PositionXFechaSigner,
                                    data.Box.FechaSigner.PositionYFechaSigner,
                                    data.Box.FechaSigner.WidthFechaSigner,
                                    data.Box.FechaSigner.HeightFechaSigner
                                )
                        ))
                        .ToList();

                    // Marcar la solicitud como completada usando el método de dominio
                    req.MarkCompleted();
                    await _db.SaveChangesAsync(cancellationToken);

                    _bus.Publish(
                        new DocumentReadyToSealEvent(
                            Guid.NewGuid(),
                            DateTime.UtcNow,
                            req.Id,
                            req.DocumentId,
                            signedImages
                        )
                    );
                }
                else
                {
                    // Obtener la primera caja del firmante actual para el evento parcial
                    var firstBox = await _db
                        .SignatureBoxes.Where(b => b.SignerId == signerId)
                        .OrderBy(b => b.PageNumber)
                        .ThenBy(b => b.PositionY)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (firstBox == null)
                    {
                        _log.LogWarning(
                            "El firmante {SignerId} no tiene cajas de firma configuradas",
                            signerId
                        );
                        return new ApiResponse<bool>(false, "Configuración de firma incompleta");
                    }

                    // Al menos un firmante pendiente ⇒ correo "firma parcial"
                    _bus.Publish(
                        new DocumentPartiallySignedEvent(
                            Guid.NewGuid(),
                            DateTime.UtcNow,
                            req.Id,
                            req.DocumentId,
                            signer.Id,
                            signer.Email!,
                            command.Payload.SignatureImageBase64,
                            firstBox.PositionX,
                            firstBox.PositionY,
                            firstBox.PageNumber,
                            signer.FullName
                        )
                    );
                }

                _log.LogInformation(
                    "Firma registrada exitosamente para el firmante {SignerId}",
                    signerId
                );
                return new ApiResponse<bool>(true, "Firma registrada exitosamente");
            }
            catch (DbUpdateConcurrencyException)
            {
                // alguien modificó la fila SignatureRequest al mismo tiempo
                return new(false, "La solicitud cambió, intenta de nuevo.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al procesar la firma");
                return new ApiResponse<bool>(false, "Error interno del servidor");
            }
        }

        private static bool IsValidPngBase64(string base64, out string clean)
        {
            clean = base64;
            var comma = base64.IndexOf(',');
            if (comma >= 0)
                clean = base64[(comma + 1)..];

            try
            {
                var bytes = Convert.FromBase64String(clean); // valida B64
                // Valida CRC pero SIN lanzar excepción al usuario:
                using var _ = Image.Load(bytes); // SixLabors.ImageSharp
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
