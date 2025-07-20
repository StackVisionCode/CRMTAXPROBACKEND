using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetSignatureBoxesHandler
    : IRequestHandler<GetSignatureBoxesQuery, ApiResponse<List<SignatureBoxListItemDto>>>
{
    private readonly ILogger<GetSignatureBoxesHandler> _logger;
    private readonly SignatureDbContext _db;

    public GetSignatureBoxesHandler(ILogger<GetSignatureBoxesHandler> logger, SignatureDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<ApiResponse<List<SignatureBoxListItemDto>>> Handle(
        GetSignatureBoxesQuery request,
        CancellationToken ct
    )
    {
        var query =
            from b in _db.SignatureBoxes
            join s in _db.Signers on b.Id equals s.Id
            join r in _db.SignatureRequests on s.SignatureRequestId equals r.Id
            select new
            {
                Box = b,
                Signer = s,
                Request = r,
            };

        var list = await (
            from s in _db.Signers
            from b in s.Boxes
            join r in _db.SignatureRequests on s.SignatureRequestId equals r.Id
            orderby s.Order, b.PageNumber, b.PositionY
            select new SignatureBoxListItemDto
            {
                Id = b.Id,
                SignerId = s.Id,
                SignatureRequestId = s.SignatureRequestId,
                DocumentId = r.DocumentId,
                SignerEmail = s.Email,
                SignerFullName = s.FullName,
                SignerOrder = s.Order,
                SignerStatus = s.Status,
                RequestStatus = r.Status,
                Page = b.PageNumber,
                PosX = b.PositionX,
                PosY = b.PositionY,
                Width = b.Width,
                Height = b.Height,
                BoxKind = b.Kind.ToString(), // solo si tienes enum Kind (agregarlo si no)
                InitialsValue = b.InitialEntity != null ? b.InitialEntity.InitalValue : null,
                DateValue = b.FechaSigner != null ? b.FechaSigner.FechaValue : null,
                SignedAtUtc = s.SignedAtUtc, // (o null si no aplica)
            }
        ).AsNoTracking().ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} signature requests.", list.Count);

        return ApiResponse<List<SignatureBoxListItemDto>>.Ok(list);
    }
}
