using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetSignersByRequestHandler
    : IRequestHandler<GetSignersByRequestQuery, ApiResponse<List<SignerDetailDto>>>
{
    private readonly ILogger<GetSignersByRequestHandler> _logger;
    private readonly SignatureDbContext _context;

    public GetSignersByRequestHandler(
        ILogger<GetSignersByRequestHandler> logger,
        SignatureDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<List<SignerDetailDto>>> Handle(
        GetSignersByRequestQuery request,
        CancellationToken cancellationToken
    )
    {
        var data = await (
            from s in _context.Signers
            where s.SignatureRequestId == request.RequestId
            select new SignerDetailDto
            {
                Id = s.Id,
                Email = s.Email,
                Order = s.Order,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                SignedAtUtc = s.SignedAtUtc,
                Boxes = s
                    .Boxes.Select(b => new SignatureBoxReadDto
                    {
                        Id = b.Id,
                        Page = b.PageNumber,
                        PosX = b.PositionX,
                        PosY = b.PositionY,
                        Width = b.Width,
                        Height = b.Height,
                        Initials = b.InitialEntity != null ? b.InitialEntity.InitalValue : null,
                        DateText = b.FechaSigner != null ? b.FechaSigner.FechaValue : null,
                    })
                    .ToList(),
            }
        ).AsNoTracking().ToListAsync(cancellationToken);

        // CORRECCIÓN: Cambiar la condición
        if (!data.Any()) // Cambiado de data.Any() a !data.Any()
        {
            _logger.LogWarning("No signers found for request {RequestId}.", request.RequestId);
            return ApiResponse<List<SignerDetailDto>>.Fail("No signers found for this request.");
        }

        _logger.LogInformation(
            "Retrieved {Count} signers for request {RequestId}.",
            data.Count,
            request.RequestId
        );
        return ApiResponse<List<SignerDetailDto>>.Ok(data);
    }
}
