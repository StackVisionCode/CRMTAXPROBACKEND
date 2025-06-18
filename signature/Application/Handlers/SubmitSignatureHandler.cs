using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;
using signature.Application.DTOs;
using signature.Infrastruture.Commands;

namespace signature.Application.Handlers
{
    public class SubmitSignatureHandler : IRequestHandler<SubmitSignatureCommand, ApiResponse<bool>>
    {
        private readonly SignatureDbContext _db;
        private readonly ISignatureValidToken _tokenSvc;

        // private readonly IEventBus _bus;
        private readonly ILogger<SubmitSignatureHandler> _log;

        public SubmitSignatureHandler(
            SignatureDbContext db,
            ISignatureValidToken tokenSvc,
            // IEventBus bus,
            ILogger<SubmitSignatureHandler> log
        )
        {
            _db = db;
            _tokenSvc = tokenSvc;
            // _bus = bus;
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
                var (isValid, signerId, reqIdStr) = _tokenSvc.Validate(
                    command.Payload.Token,
                    "sign"
                );
                if (!isValid)
                    return new ApiResponse<bool>(false, "Token inválido o expirado");

                var reqId = Guid.Parse(reqIdStr);

                // 2. Obtener solicitud y firmante
                var req = await _db
                    .SignatureRequests.Include(r => r.Signers)
                    .FirstOrDefaultAsync(r => r.Id == reqId, cancellationToken);

                if (req == null)
                    return new ApiResponse<bool>(false, "Solicitud de firma no encontrada");

                var signer = req.Signers.FirstOrDefault(x => x.Id == signerId);
                if (signer == null)
                    return new ApiResponse<bool>(false, "Firmante no encontrado");

                if (signer.Status == SignerStatus.Signed)
                    return new ApiResponse<bool>(false, "Esta firma ya ha sido registrada");

                // 3. Validar certificado
                if (command.Payload.Certificate.NotAfter < DateTime.UtcNow)
                    return new ApiResponse<bool>(false, "El certificado digital ha expirado");

                // 4. Crear certificado digital
                var cert = new DigitalCertificate(
                    command.Payload.Certificate.Thumbprint,
                    command.Payload.Certificate.Subject,
                    command.Payload.Certificate.NotBefore,
                    command.Payload.Certificate.NotAfter
                );

                // 5. Registrar firma (solo metadatos en la BD)
                req.ReceiveSignature(signerId, command.Payload.SignatureImageBase64, cert);
                await _db.SaveChangesAsync(cancellationToken);

                // 6. Publicar evento para que CloudShield procese el PDF
                // _bus.Publish(new DocumentPartiallySignedEvent(
                //     Guid.NewGuid(),
                //     DateTime.UtcNow,
                //     req.Id,
                //     req.DocumentId,
                //     signer.Id,
                //     command.Payload.SignatureImageBase64,
                //     signer.PositionX,
                //     signer.PositionY,
                //     signer.PageNumber,
                //     command.Payload.Certificate
                // ));

                // 7. Si todas las firmas están completas
                // if (req.Status == SignatureStatus.Completed)
                // {
                //     var allSignatures = req.Signers
                //         .Where(s => s.Status == SignerStatus.Signed)
                //         .Select(s => new SignatureDataDto
                //         {
                //             SignerId = s.Id,
                //             SignatureImageBase64 = s.SignatureImage!,
                //             PositionX = s.PositionX,
                //             PositionY = s.PositionY,
                //             PageNumber = s.PageNumber,
                //             Order = s.Order,
                //             Certificate = new DigitalCertificateDto
                //             {
                //                 Thumbprint = s.Certificate!.Thumbprint,
                //                 Subject = s.Certificate.Subject,
                //                 NotBefore = s.Certificate.NotBefore,
                //                 NotAfter = s.Certificate.NotAfter
                //             }
                //         }).ToList();

                //     _bus.Publish(new DocumentFullySignedEvent(
                //         Guid.NewGuid(),
                //         DateTime.UtcNow,
                //         req.Id,
                //         req.DocumentId,
                //         allSignatures
                //     ));
                // }

                _log.LogInformation(
                    "Firma registrada exitosamente para el firmante {SignerId}",
                    signerId
                );
                return new ApiResponse<bool>(true, "Firma registrada exitosamente");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al procesar la firma");
                return new ApiResponse<bool>(false, "Error interno del servidor");
            }
        }
    }
}
