using AutoMapper;
using CommLinkService.Application.Commands;
using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using Common;
using DTOs.RoomDTOs;
using MediatR;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, ApiResponse<RoomDTO>>
{
    private readonly ICommLinkDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateRoomHandler> _logger;

    public CreateRoomHandler(
        ICommLinkDbContext context,
        IMapper mapper,
        IEventBus eventBus,
        ILogger<CreateRoomHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ApiResponse<RoomDTO>> Handle(
        CreateRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Crear Room con nueva estructura
            var room = new Room
            {
                Id = Guid.NewGuid(),
                Name = request.RoomData.Name,
                Type = request.RoomData.Type,
                CreatedByCompanyId = request.RoomData.CreatedByCompanyId,
                CreatedByTaxUserId = request.RoomData.CreatedByTaxUserId,
                MaxParticipants = request.RoomData.MaxParticipants,
                IsActive = true,
                LastActivityAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Rooms.Add(room);

            // Agregar creador como Owner (TaxUser)
            var creatorParticipant = new RoomParticipant
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                ParticipantType = ParticipantType.TaxUser,
                TaxUserId = request.RoomData.CreatedByTaxUserId,
                CompanyId = request.RoomData.CreatedByCompanyId,
                AddedByCompanyId = request.RoomData.CreatedByCompanyId,
                AddedByTaxUserId = request.RoomData.CreatedByTaxUserId,
                Role = ParticipantRole.Owner,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.RoomParticipants.Add(creatorParticipant);

            // Agregar Customer como Member si se especifica
            if (request.RoomData.CustomerId.HasValue)
            {
                var customerParticipant = new RoomParticipant
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    ParticipantType = ParticipantType.Customer,
                    CustomerId = request.RoomData.CustomerId.Value,
                    CompanyId = null, // Customers no tienen CompanyId
                    AddedByCompanyId = request.RoomData.CreatedByCompanyId,
                    AddedByTaxUserId = request.RoomData.CreatedByTaxUserId,
                    Role = ParticipantRole.Member,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.RoomParticipants.Add(customerParticipant);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var roomDto = _mapper.Map<RoomDTO>(room);

            _eventBus.Publish(
                new RoomCreatedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    room.Id,
                    room.Name,
                    room.Type,
                    request.RoomData.CreatedByTaxUserId,
                    request.RoomData.CustomerId.HasValue
                        ? new List<Guid> { request.RoomData.CustomerId.Value }
                        : new List<Guid>()
                )
            );

            _logger.LogInformation(
                "Room {RoomId} created by TaxUser {TaxUserId} from Company {CompanyId}",
                room.Id,
                request.RoomData.CreatedByTaxUserId,
                request.RoomData.CreatedByCompanyId
            );

            return new ApiResponse<RoomDTO>(true, "Room created successfully", roomDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return new ApiResponse<RoomDTO>(false, "Failed to create room");
        }
    }
}
