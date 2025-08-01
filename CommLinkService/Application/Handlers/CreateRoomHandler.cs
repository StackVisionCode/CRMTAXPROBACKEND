using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, CreateRoomResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CreateRoomHandler> _logger;

    public CreateRoomHandler(
        ICommLinkDbContext context,
        IEventBus eventBus,
        ILogger<CreateRoomHandler> logger
    )
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<CreateRoomResult> Handle(
        CreateRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        var room = new Room(request.Name, request.Type, request.CreatorId, request.MaxParticipants);

        // Add creator as owner
        room.AddParticipant(request.CreatorId, ParticipantRole.Owner);

        // Add other participants if specified
        if (request.ParticipantIds?.Any() == true)
        {
            foreach (var participantId in request.ParticipantIds)
            {
                if (participantId != request.CreatorId)
                {
                    room.AddParticipant(participantId, ParticipantRole.Member);
                }
            }
        }

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish event
        _eventBus.Publish(
            new RoomCreatedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                room.Id,
                room.Name,
                room.Type,
                request.CreatorId,
                request.ParticipantIds ?? new List<Guid>()
            )
        );

        _logger.LogInformation(
            "Room {RoomId} created by user {UserId}",
            room.Id,
            request.CreatorId
        );

        return new CreateRoomResult(room.Id, room.Name, room.Type, room.CreatedAt);
    }
}
