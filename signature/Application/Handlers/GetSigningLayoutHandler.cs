using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Application.Handlers;

public class GetSigningLayoutHandler
    : IRequestHandler<GetSigningLayoutQuery, ApiResponse<SigningLayoutDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<GetSigningLayoutHandler> _log;

    public GetSigningLayoutHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<GetSigningLayoutHandler> log
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
    }

    public async Task<ApiResponse<SigningLayoutDto>> Handle(
        GetSigningLayoutQuery request,
        CancellationToken ct
    )
    {
        // 1. Validar token (scope "sign")
        var (ok, signerId, requestId) = _tokenSvc.Validate(request.Token, "sign");
        if (!ok)
            return new(false, "Token inválido o expirado");

        // 2. Obtener estado básico de la solicitud
        var reqRow = await _db
            .SignatureRequests.Where(r => r.Id == requestId)
            .Select(r => new
            {
                r.Id,
                r.DocumentId,
                r.Status,
            })
            .FirstOrDefaultAsync(ct);

        if (reqRow is null)
            return new(false, "Solicitud no encontrada");

        if (reqRow.Status == SignatureStatus.Rejected)
            return new(false, "La solicitud fue rechazada.");

        // 3. Obtener SÓLO el firmante actual con sus boxes (owned)
        var signerProjection = await _db
            .Signers.Where(s => s.Id == signerId && s.SignatureRequestId == requestId)
            .Select(s => new
            {
                s.Id,
                s.Order,
                s.Status,
                s.SignedAtUtc,
                s.FullName,
                Boxes = s.Boxes.Select(b => new
                {
                    b.Id,
                    b.PageNumber,
                    b.PositionX,
                    b.PositionY,
                    b.Width,
                    b.Height,
                    InitialValue = b.InitialEntity != null ? b.InitialEntity.InitalValue : null,
                    DateValue = b.FechaSigner != null ? b.FechaSigner.FechaValue : null,
                }),
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (signerProjection is null)
            return new(false, "Firmante no encontrado");

        // 4. Mapear sólo sus cajas
        var boxDtos = signerProjection
            .Boxes.Select(b =>
            {
                var kind = b.InitialValue is not null
                    ? BoxKind.Initials
                    : (b.DateValue is not null ? BoxKind.Date : BoxKind.Signature);

                return new SigningBoxDto
                {
                    BoxId = b.Id,
                    Page = b.PageNumber,
                    PosX = b.PositionX,
                    PosY = b.PositionY,
                    Width = b.Width,
                    Height = b.Height,
                    Kind = kind,
                    SignerId = signerProjection.Id,
                    SignerOrder = signerProjection.Order,
                    SignerStatus = signerProjection.Status,
                    IsCurrentSigner = true,
                    SignedAtUtc = signerProjection.SignedAtUtc,
                    InitialsValue = b.InitialValue,
                    DateValue = b.DateValue,
                };
            })
            .OrderBy(x => x.Page)
            .ToList();

        var dto = new SigningLayoutDto
        {
            SignatureRequestId = reqRow.Id,
            DocumentId = reqRow.DocumentId,
            RequestStatus = reqRow.Status,
            CurrentSignerId = signerProjection.Id,
            CurrentSignerOrder = signerProjection.Order,
            Boxes = boxDtos,
        };

        _log.LogInformation(
            "Layout seguro devuelto para Request {Req} - Signer {Signer} con {Cnt} boxes",
            reqRow.Id,
            signerProjection.Id,
            dto.Boxes.Count
        );

        return new(true, "Layout obtenido", dto);
    }
}
