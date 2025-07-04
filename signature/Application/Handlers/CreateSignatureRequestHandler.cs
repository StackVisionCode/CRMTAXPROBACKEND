using Application.Helpers;
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
    private readonly GetOriginURL _getOriginURL;

    public CreateSignatureRequestHandler(
        SignatureDbContext db,
        ISignatureValidToken tokens,
        IEventBus bus,
        ILogger<CreateSignatureRequestHandler> log,
        GetOriginURL getOriginURL
    )
    {
        _db = db;
        _tokens = tokens;
        _bus = bus;
        _log = log;
        _getOriginURL = getOriginURL;
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
        foreach (var inDto in cmd.Payload.Signers)
        {
            Guid signerId = Guid.NewGuid(); // ► PK real de la fila ‘Signer’
            var (token, exp) = _tokens.Generate(signerId, req.Id, "sign");

            req.AddSigner(
                signerId, // ← mismo Guid en el JWT (sub)
                inDto.CustomerId ?? Guid.Empty,
                inDto.Email,
                inDto.Order,
                inDto.Page,
                inDto.PosX,
                inDto.PosY,
                inDto.Width,
                inDto.Height,
                inDto.InitialEntity is null
                    ? null
                    : new IntialEntity(
                        inDto.InitialEntity.InitalValue,
                        inDto.InitialEntity.WidthIntial,
                        inDto.InitialEntity.HeightIntial,
                        inDto.InitialEntity.PositionXIntial,
                        inDto.InitialEntity.PositionYIntial
                    ),
                inDto.FechaSigner is null
                    ? null
                    : new FechaSigner(
                        inDto.FechaSigner.FechaValue,
                        inDto.FechaSigner.WidthFechaSigner,
                        inDto.FechaSigner.HeightFechaSigner,
                        inDto.FechaSigner.PositionXFechaSigner,
                        inDto.FechaSigner.PositionYFechaSigner
                    ),
                token
            ); // ← se persiste para auditoría

            _log.LogInformation("InitialEntity: {@InitialEntity}", inDto.InitialEntity);

            pendingEvents.Add(
                new SignatureInvitationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    signerId, // sub
                    inDto.Email,
                    $"{baseUrl}/firmar?token={token}",
                    exp
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
