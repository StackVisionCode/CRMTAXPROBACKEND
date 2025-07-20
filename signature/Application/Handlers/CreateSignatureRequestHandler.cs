using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Context;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;
using SharedLibrary.Helpers;
using signature.Infrastruture.Commands;

public sealed class CreateSignatureRequestHandler
    : IRequestHandler<CreateSignatureRequestCommand, ApiResponse<bool>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokens;
    private readonly IEventBus _bus;
    private readonly ILogger<CreateSignatureRequestHandler> _log;
    private readonly IMapper _mapper;
    private readonly GetOriginURL _getOriginURL;

    public CreateSignatureRequestHandler(
        SignatureDbContext db,
        ISignatureValidToken tokens,
        IEventBus bus,
        ILogger<CreateSignatureRequestHandler> log,
        GetOriginURL getOriginURL,
        IMapper mapper
    )
    {
        _db = db;
        _tokens = tokens;
        _bus = bus;
        _log = log;
        _getOriginURL = getOriginURL;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateSignatureRequestCommand cmd,
        CancellationToken ct
    )
    {
        /* ╭──────────────────────────────────────────────────────────────╮
        │ 1 ▸ nueva solicitud de firma                                │
           ╰──────────────────────────────────────────────────────────────╯ */
        var req = new SignatureRequest(cmd.Payload.DocumentId, Guid.NewGuid());

        // 1.1 resuelve UNA sola vez la URL base para toda la solicitud
        var baseUrl = _getOriginURL.GetOrigin(); // ej. https://app.company.com

        // guardamos los eventos para publicarlos DESPUÉS del commit
        var pendingEvents = new List<SignatureInvitationEvent>();

        /* ╭──────────────────────────────────────────────────────────────╮
        │ 2 ▸ iteramos firmantes, generamos token y los agregamos      │
           ╰──────────────────────────────────────────────────────────────╯ */
        foreach (var sDto in cmd.Payload.Signers)
        {
            Guid signerId = Guid.NewGuid(); // ► PK real de la fila 'Signer'
            var (token, exp) = _tokens.Generate(signerId, req.Id, "sign");

            var signer = new Signer(
                signerId,
                sDto.CustomerId,
                sDto.Email,
                sDto.Order,
                req.Id,
                token,
                sDto.FullName
            );

            // Crear las cajas manualmente con el SignerId correcto
            var boxes = new List<SignatureBox>();
            foreach (var boxDto in sDto.Boxes)
            {
                var initialEntity = _mapper.Map<IntialEntity?>(boxDto.InitialEntity);
                var fechaSigner = _mapper.Map<FechaSigner?>(boxDto.FechaSigner);
                var kind = SignatureBox.DetermineKind(initialEntity, fechaSigner);

                var box = new SignatureBox(
                    signerId, // Ahora tenemos el SignerId
                    boxDto.Page,
                    boxDto.PosX,
                    boxDto.PosY,
                    boxDto.Width,
                    boxDto.Height,
                    kind,
                    initialEntity,
                    fechaSigner
                );

                boxes.Add(box);
            }

            // Agregar todas las cajas al signer
            signer.AddBoxes(boxes);

            // agregamos el firmante a la solicitud
            req.AttachSigner(signer);

            // log para depuración
            _log.LogInformation(
                "Creando signer {SignerId} con {BoxCount} cajas",
                signerId,
                boxes.Count
            );

            pendingEvents.Add(
                new SignatureInvitationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    signerId, // sub
                    sDto.Email,
                    $"{baseUrl}/customer-signature?token={token}",
                    exp,
                    sDto.FullName
                )
            );
        }

        /* ╭──────────────────────────────────────────────────────────────╮
        │ 3 ▸ persistir dentro de una transacción                     │
           ╰──────────────────────────────────────────────────────────────╯ */
        await using var trx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.SignatureRequests.AddAsync(req, ct);
            await _db.SaveChangesAsync(ct);
            await trx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync(ct);
            _log.LogError(ex, "❌ Error al guardar la SignatureRequest");
            return new ApiResponse<bool>(false, "No se pudo crear la solicitud");
        }

        /* ╭──────────────────────────────────────────────────────────────╮
        │ 4 ▸ ahora sí: publicar invitaciones por RabbitMQ            │
           ╰──────────────────────────────────────────────────────────────╯ */
        foreach (var ev in pendingEvents)
            _bus.Publish(ev);

        _log.LogInformation(
            "✅ SignatureRequest {Id} creada con {Cnt} firmantes",
            req.Id,
            req.Signers.Count
        );

        return new ApiResponse<bool>(true, "Solicitud creada");
    }
}
