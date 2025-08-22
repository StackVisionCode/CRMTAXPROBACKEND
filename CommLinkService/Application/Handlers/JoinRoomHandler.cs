using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.RoomDTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class JoinRoomHandler
    : IRequestHandler<JoinRoomCommand, ApiResponse<RoomParticipantDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<JoinRoomHandler> _logger;

    public JoinRoomHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        ILogger<JoinRoomHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<RoomParticipantDTO>> Handle(
        JoinRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el room existe
            var room = await _context
                .Rooms.Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsActive, cancellationToken);

            if (room == null)
                return new ApiResponse<RoomParticipantDTO>(false, "Room not found");

            // Verificar capacidad
            var activeParticipants = room.Participants.Count(p => p.IsActive);
            if (activeParticipants >= room.MaxParticipants)
                return new ApiResponse<RoomParticipantDTO>(false, "Room is full");

            // Buscar participante existente
            RoomParticipant? existingParticipant = null;
            if (request.ParticipantType == ParticipantType.TaxUser)
            {
                existingParticipant = room.Participants.FirstOrDefault(p =>
                    p.ParticipantType == ParticipantType.TaxUser && p.TaxUserId == request.TaxUserId
                );
            }
            else if (request.ParticipantType == ParticipantType.Customer)
            {
                existingParticipant = room.Participants.FirstOrDefault(p =>
                    p.ParticipantType == ParticipantType.Customer
                    && p.CustomerId == request.CustomerId
                );
            }

            if (existingParticipant != null)
            {
                if (existingParticipant.IsActive)
                {
                    var existingDto = _mapper.Map<RoomParticipantDTO>(existingParticipant);
                    return new ApiResponse<RoomParticipantDTO>(
                        true,
                        "Already in room",
                        existingDto
                    );
                }

                // Reactivar participante
                existingParticipant.IsActive = true;
                existingParticipant.JoinedAt = DateTime.UtcNow;
                existingParticipant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                var reactivatedDto = _mapper.Map<RoomParticipantDTO>(existingParticipant);
                return new ApiResponse<RoomParticipantDTO>(true, "Rejoined room", reactivatedDto);
            }

            // Crear nuevo participante
            var newParticipant = new RoomParticipant
            {
                Id = Guid.NewGuid(),
                RoomId = request.RoomId,
                ParticipantType = request.ParticipantType,
                TaxUserId = request.TaxUserId,
                CustomerId = request.CustomerId,
                CompanyId = request.CompanyId,
                AddedByCompanyId = request.CompanyId ?? Guid.Empty, // Temporal
                AddedByTaxUserId = request.TaxUserId ?? Guid.Empty, // Temporal
                Role = ParticipantRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.RoomParticipants.Add(newParticipant);
            await _context.SaveChangesAsync(cancellationToken);

            var participantDto = _mapper.Map<RoomParticipantDTO>(newParticipant);

            _logger.LogInformation(
                "{ParticipantType} {UserId} joined room {RoomId}",
                request.ParticipantType,
                request.ParticipantType == ParticipantType.TaxUser
                    ? request.TaxUserId
                    : request.CustomerId,
                request.RoomId
            );

            return new ApiResponse<RoomParticipantDTO>(
                true,
                "Joined room successfully",
                participantDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room");
            return new ApiResponse<RoomParticipantDTO>(false, "Failed to join room");
        }
    }
}
