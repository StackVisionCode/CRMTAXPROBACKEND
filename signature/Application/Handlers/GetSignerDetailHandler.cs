using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetSignerDetailHandler
    : IRequestHandler<GetSignerDetailQuery, ApiResponse<SignerDetailDto>>
{
    private readonly ILogger<GetSignerDetailHandler> _logger;
    private readonly SignatureDbContext _context;

    public GetSignerDetailHandler(
        ILogger<GetSignerDetailHandler> logger,
        SignatureDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<SignerDetailDto>> Handle(
        GetSignerDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        // 1. Obtener la información del firmante
        var signerInfo = await _context
            .Signers.Where(signer => signer.Id == request.SignerId)
            .Select(signer => new
            {
                signer.Id,
                signer.Email,
                signer.Order,
                signer.Status,
                signer.CreatedAt,
                signer.SignedAtUtc,
                signer.RejectReason,
                signer.RejectedAtUtc,
                signer.FullName,
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (signerInfo == null)
        {
            _logger.LogWarning("Signer with ID {SignerId} not found.", request.SignerId);
            return ApiResponse<SignerDetailDto>.Fail(
                $"Signer with ID {request.SignerId} not found."
            );
        }

        // 2. Obtener las cajas del firmante por separado
        var signerBoxes = await _context
            .SignatureBoxes.Where(b => b.SignerId == request.SignerId)
            .OrderBy(b => b.PageNumber)
            .ThenBy(b => b.PositionY)
            .Select(b => new SignatureBoxReadDto
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
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 3. Construir el DTO final
        var result = new SignerDetailDto
        {
            Id = signerInfo.Id,
            Email = signerInfo.Email,
            Order = signerInfo.Order,
            Status = signerInfo.Status,
            CreatedAt = signerInfo.CreatedAt,
            SignedAtUtc = signerInfo.SignedAtUtc,
            RejectedReason = signerInfo.RejectReason ?? string.Empty,
            RejectedAtUtc = signerInfo.RejectedAtUtc,
            FullName = signerInfo.FullName,
            Boxes = signerBoxes,
        };

        _logger.LogInformation(
            "Successfully retrieved signer detail for SignerId: {SignerId} with {BoxCount} boxes",
            request.SignerId,
            signerBoxes.Count
        );

        return ApiResponse<SignerDetailDto>.Ok(result, "Signer detail retrieved successfully.");
    }
}
