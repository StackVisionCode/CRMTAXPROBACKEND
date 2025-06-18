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
    IEventBus bus,
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
            var req = new SignatureRequest(c.Payload.DocumentId, Guid.NewGuid());

            // 2. Agregar firmantes
            foreach (var signer in c.Payload.Signers)
            {
                Guid signerId = Guid.NewGuid();
                var (token, exp) = tokenSvc.Generate(signerId, req.Id, "sign");
                req.AddSigner(
                    signerId,
                    signer.CustomerId,
                    signer.Email,
                    signer.Order,
                    signer.Page,
                    signer.PosX,
                    signer.PosY,
                    signer.Token = token
                );

                string link = $"http://localhost:4200/firmar?token={token}";

                bus.Publish(
                    new SignatureInvitationEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        signer.CustomerId,
                        signer.Email,
                        link,
                        exp
                    )
                );
            }

            db.SignatureRequests.AddRange(req);
            await db.SaveChangesAsync(ct);

            log.LogInformation(
                "SignatureRequest {Id} creada con {Cnt} firmantes",
                req.Id,
                req.Signers.Count
            );
            return new(true, "Solicitud creada");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear la solicitud de firma");
            return new(false, "Error al crear la solicitud de firma");
        }
    }
}
