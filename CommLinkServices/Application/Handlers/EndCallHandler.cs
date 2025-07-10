using CommLinkServices.Application.DTOs;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents;

namespace CommLinkServices.Application.Handlers;

public class EndCallHandler : IRequestHandler<EndCallCommand, ApiResponse<CallEndedDto>>
{
    private readonly ILogger<EndCallHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IHubContext<CommHub> _hub;
    private readonly IEventBus _bus;

    public EndCallHandler(
        ILogger<EndCallHandler> logger,
        CommLinkDbContext db,
        IHubContext<CommHub> hub,
        IEventBus bus
    )
    {
        _logger = logger;
        _db = db;
        _hub = hub;
        _bus = bus;
    }

    public async Task<ApiResponse<CallEndedDto>> Handle(
        EndCallCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var call = await _db.Calls.FirstOrDefaultAsync(
                c =>
                    c.Id == request.Payload.CallId
                    && c.ConversationId == request.Payload.ConversationId,
                cancellationToken
            );

            if (call == null)
                return new(false, "Call not found");

            if (call.EndedAt is not null)
            {
                var durationCached = (int)(call.EndedAt.Value - call.StartedAt).TotalSeconds;
                var cachedDto = new CallEndedDto
                {
                    ConversationId = call.ConversationId,
                    CallId = call.Id,
                    EndedById = request.RequesterId,
                    DurationSeconds = durationCached,
                    EndedAt = call.EndedAt.Value,
                };
                return new ApiResponse<CallEndedDto>(true, "Ok", cachedDto);
            }

            call.EndedAt = DateTime.UtcNow;
            call.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            int duration = (int)(call.EndedAt.Value - call.StartedAt).TotalSeconds;

            _bus.Publish(
                new CallEndedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    call.ConversationId,
                    call.Id,
                    request.RequesterId,
                    duration
                )
            );

            await _hub
                .Clients.Group($"convo-{call.ConversationId}")
                .SendAsync(
                    "CallEnded",
                    new
                    {
                        call.Id,
                        call.EndedAt,
                        duration,
                    },
                    cancellationToken
                );

            var dto = new CallEndedDto
            {
                ConversationId = call.ConversationId,
                CallId = call.Id,
                EndedById = request.RequesterId,
                DurationSeconds = duration,
                EndedAt = call.EndedAt.Value,
            };

            return new ApiResponse<CallEndedDto>(true, "Ok", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EndCallHandler.Handle: {Message}", ex.Message);
            return new ApiResponse<CallEndedDto>(false, ex.Message);
        }
    }
}
