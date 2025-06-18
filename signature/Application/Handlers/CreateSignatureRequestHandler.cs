using Application.Helpers;
using AutoMapper;
using Infrastructure.Context;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;
using signature.Infrastruture.Commands;

public class CreateSignatureRequestHandler(
    SignatureDbContext db,
    // IMapper mapper,
    ISignatureValidToken tokenSvc,
    // IEventBus bus,
    ILogger<CreateSignatureRequestHandler> log
) : IRequestHandler<CreateSignatureRequestCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        CreateSignatureRequestCommand c,
        CancellationToken ct
    )
    {
        try
        {
            // 1. Crear la solicitud de firma
            var req = new SignatureRequest(c.Payload.DocumentId, c.Payload.Id = Guid.NewGuid());

            // 2. Agregar firmantes
            foreach (var signer in c.Payload.Signers)
            {
                var (token, exp) = tokenSvc.Generate(signer.CustomerId, req.Id.ToString(), "sign");
                req.AddSigner(
                    signer.CustomerId,
                    signer.Email,
                    signer.Order,
                    signer.Page,
                    signer.PosX,
                    signer.PosY,
                    signer.Token = token
                );
            }

            db.SignatureRequests.AddRange(req);
            await db.SaveChangesAsync(ct);

            // 3. Generar tokens y enviar invitaciones
            // foreach (var signer in req.Signers)
            // {
            //     // string link = $"http://localhost:4200/firmar?token={token}";

            //     // bus.Publish(
            //     //     new SignatureInvitationEvent(
            //     //         Guid.NewGuid(),
            //     //         DateTime.UtcNow,
            //     //         signer.Id,
            //     //         signer.Email,
            //     //         link,
            //     //         exp
            //     //     )
            //     // );
            // }

            log.LogInformation("SignatureRequest {Id} creada", req.Id);
            return new(true, "Solicitud creada");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear la solicitud de firma");
            return new(false, "Error al crear la solicitud de firma");
        }
    }
}
