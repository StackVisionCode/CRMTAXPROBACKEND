using CommLinkServices.Domain;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents;

namespace CommLinkServices.Application.Handlers;

public class StartCallHandler : IRequestHandler<StartCallCommand, ApiResponse<Guid>>
{
    private readonly ILogger<StartCallHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IHubContext<CommHub> _hub;
    private readonly IEventBus _bus;

    public StartCallHandler(
        ILogger<StartCallHandler> logger,
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

    public async Task<ApiResponse<Guid>> Handle(
        StartCallCommand request,
        CancellationToken cancellationToken
    )
    {
        var call = new Call
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            StarterId = request.StarterId,
            Type = Enum.Parse<CallType>(request.Payload.CallType, true),
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        await _db.Calls.AddAsync(call, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _bus.Publish(
            new CallStartedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.ConversationId,
                call.Id,
                request.StarterId,
                call.Type.ToString()
            )
        );

        await _hub
            .Clients.Group($"convo-{request.ConversationId}")
            .SendAsync(
                "CallStarted",
                new
                {
                    call.Id,
                    call.StarterId,
                    call.Type,
                    call.StartedAt,
                },
                cancellationToken
            );

        return new(true, "Call started", call.Id);
    }
}
