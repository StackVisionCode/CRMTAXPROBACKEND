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

                // reqId ya es Guid
                var req = await _db
                    .SignatureRequests.Include(r => r.Signers)
                    .FirstOrDefaultAsync(r => r.Id == reqId, cancellationToken);

                if (req == null)
                    return new ApiResponse<bool>(false, "Solicitud de firma no encontrada");

                var signer = req.Signers.FirstOrDefault(x => x.Id == signerId);
                if (signer == null)
                    return new ApiResponse<bool>(false, "Firmante no encontrado");

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

                // 5. Registrar firma (solo metadatos en la BD)
                req.ReceiveSignature(
                    signerId,
                    cleanB64,
                    cert,
                    command.Payload.SignedAtUtc,
                    command.Payload.ClientIp,
                    command.Payload.UserAgent,
                    command.Payload.ConsentAgreedAtUtc,
                    command.Payload.Consent_text,
                    command.Payload.Consent_button_text
                );
                await _db.SaveChangesAsync(cancellationToken);

                //6 ▸ ¿faltan firmas o ya está completo?

                bool hasPending = req.Signers.Any(s => s.Status == SignerStatus.Pending);

                if (!hasPending)
                {
                    var signedImages = req
                        .Signers.SelectMany(s =>
                            s.Boxes.Select(b => new SignedImageDto(
                                s.CustomerId ?? Guid.Empty,
                                s.Id,
                                s.Email!,
                                b.PageNumber,
                                b.PositionX,
                                b.PositionY,
                                b.Width,
                                b.Height,
                                (b.InitialEntity is null && b.FechaSigner is null)
                                    ? s.SignatureImage
                                    : null,
                                s.Certificate!.Thumbprint,
                                s.SignedAtUtc!.Value,
                                s.ClientIp ?? string.Empty,
                                s.UserAgent ?? string.Empty,
                                s.ConsentAgreedAtUtc!.Value,
                                b.InitialEntity is null
                                    ? null
                                    : new InitialStampDto(
                                        b.InitialEntity.InitalValue,
                                        b.InitialEntity.PositionXIntial,
                                        b.InitialEntity.PositionYIntial,
                                        b.InitialEntity.WidthIntial,
                                        b.InitialEntity.HeightIntial
                                    ),
                                b.FechaSigner is null
                                    ? null
                                    : new DateStampDto(
                                        b.FechaSigner.FechaValue,
                                        b.FechaSigner.PositionXFechaSigner,
                                        b.FechaSigner.PositionYFechaSigner,
                                        b.FechaSigner.WidthFechaSigner,
                                        b.FechaSigner.HeightFechaSigner
                                    )
                            ))
                        )
                        .ToList();

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
                    var firstBox = signer.Boxes.First();

                    // Al menos un firmante pendiente ⇒ correo “firma parcial”
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
