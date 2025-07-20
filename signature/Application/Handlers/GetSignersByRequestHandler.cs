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
        // 1. Obtener todos los firmantes de la solicitud
        var signers = await _context
            .Signers.Where(s => s.SignatureRequestId == request.RequestId)
            .OrderBy(s => s.Order)
            .Select(s => new
            {
                s.Id,
                s.Email,
                s.Order,
                s.Status,
                s.CreatedAt,
                s.SignedAtUtc,
                s.RejectReason,
                s.RejectedAtUtc,
                s.FullName,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!signers.Any())
        {
            _logger.LogWarning("No signers found for request {RequestId}.", request.RequestId);
            return ApiResponse<List<SignerDetailDto>>.Fail("No signers found for this request.");
        }

        // 2. Obtener todas las cajas para estos firmantes en una sola consulta
        var signerIds = signers.Select(s => s.Id).ToList();
        var allBoxes = await _context
            .SignatureBoxes.Where(b => signerIds.Contains(b.SignerId))
            .OrderBy(b => b.PageNumber)
            .ThenBy(b => b.PositionY)
            .Select(b => new
            {
                b.SignerId,
                Box = new SignatureBoxReadDto
                {
                    Id = b.Id,
                    Page = b.PageNumber,
                    PosX = b.PositionX,
                    PosY = b.PositionY,
                    Width = b.Width,
                    Height = b.Height,
                    Initials = b.InitialEntity != null ? b.InitialEntity.InitalValue : null,
                    DateText = b.FechaSigner != null ? b.FechaSigner.FechaValue : null,
                },
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 3. Agrupar las cajas por firmante
        var boxesBySignerId = allBoxes
            .GroupBy(b => b.SignerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Box).ToList());

        // 4. Construir la lista final de SignerDetailDto
        var result = signers
            .Select(s => new SignerDetailDto
            {
                Id = s.Id,
                Email = s.Email,
                Order = s.Order,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                SignedAtUtc = s.SignedAtUtc,
                RejectedReason = s.RejectReason ?? string.Empty,
                RejectedAtUtc = s.RejectedAtUtc,
                FullName = s.FullName,
                Boxes = boxesBySignerId.GetValueOrDefault(s.Id, new List<SignatureBoxReadDto>()),
            })
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} signers for request {RequestId} with total {BoxCount} boxes.",
            result.Count,
            request.RequestId,
            allBoxes.Count
        );

        return ApiResponse<List<SignerDetailDto>>.Ok(result);
    }
}
