using AutoMapper;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Domain;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents;

namespace CommLinkServices.Infrastructure.Handlers;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, ApiResponse<MessageDto>>
{
    private readonly ILogger<SendMessageHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IMapper _mapper;
    private readonly IHubContext<CommHub> _hub;
    private readonly IEventBus _bus;

    public SendMessageHandler(
        ILogger<SendMessageHandler> logger,
        CommLinkDbContext db,
        IMapper mapper,
        IHubContext<CommHub> hub,
        IEventBus bus
    )
    {
        _logger = logger;
        _db = db;
        _mapper = mapper;
        _hub = hub;
        _bus = bus;
    }

    public async Task<ApiResponse<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var belongs = await _db.Conversations.AnyAsync(
                c =>
                    c.Id == request.ConversationId
                    && (c.FirstUserId == request.SenderId || c.SecondUserId == request.SenderId),
                cancellationToken
            );
            if (!belongs)
                return new(false, "Forbidden");

            var message = _mapper.Map<Message>(request.Payload);
            message.Id = Guid.NewGuid();
            message.ConversationId = request.ConversationId;
            message.SenderId = request.SenderId;
            message.SentAt = DateTime.UtcNow;
            message.CreatedAt = DateTime.UtcNow;
            await _db.Messages.AddAsync(message, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _bus.Publish(
                new MessageSentEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    request.ConversationId,
                    request.SenderId,
                    message.Content,
                    message.HasAttachment,
                    message.AttachmentUrl
                )
            );

            var dto = _mapper.Map<MessageDto>(message);
            await _hub
                .Clients.Group($"convo-{request.ConversationId}")
                .SendAsync("ReceiveMessage", dto, cancellationToken);
            return new ApiResponse<MessageDto>(true, "Message sent", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message");
            return new ApiResponse<MessageDto>(false, "Failed to send message");
        }
    }
}
