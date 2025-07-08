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
        var s = await (
            from signer in _context.Signers
            where signer.Id == request.SignerId
            select new SignerDetailDto
            {
                Id = signer.Id,
                Email = signer.Email,
                Order = signer.Order,
                Status = signer.Status,
                CreatedAt = signer.CreatedAt,
                SignedAtUtc = signer.SignedAtUtc,
                Boxes = signer
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
        ).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved signer detail for SignerId: {SignerId}",
            request.SignerId
        );

        if (s == null)
        {
            _logger.LogWarning("Signer with ID {SignerId} not found.", request.SignerId);
            return ApiResponse<SignerDetailDto>.Fail(
                $"Signer with ID {request.SignerId} not found."
            );
        }

        _logger.LogInformation(
            "Successfully retrieved signer detail for SignerId: {SignerId}",
            request.SignerId
        );

        return ApiResponse<SignerDetailDto>.Ok(s, "Signer detail retrieved successfully.");
    }
}
