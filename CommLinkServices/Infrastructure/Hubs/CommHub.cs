using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CommLinkServices.Infrastructure.Hubs;

[Authorize]
public class CommHub : Hub
{
    private readonly ILogger<CommHub> _logger;

    public CommHub(ILogger<CommHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // El “sub” del JWT: asegúrate de configurar UserIdProvider
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation(
                "Socket {Conn} joined personal group user-{User}",
                Context.ConnectionId,
                userId
            );
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnDisconnectedAsync(ex);
    }

    public async Task JoinConversation(Guid conversationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"convo-{conversationId}");

    public async Task LeaveConversation(Guid conversationId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"convo-{conversationId}");

    // WebRTC signalling
    public async Task SendSdpOffer(Guid convoId, object offer) =>
        await Clients.OthersInGroup($"convo-{convoId}").SendAsync("ReceiveSdpOffer", offer);

    public async Task SendSdpAnswer(Guid convoId, object answer) =>
        await Clients.OthersInGroup($"convo-{convoId}").SendAsync("ReceiveSdpAnswer", answer);

    public async Task SendIceCandidate(Guid convoId, object cand) =>
        await Clients.OthersInGroup($"convo-{convoId}").SendAsync("ReceiveIceCandidate", cand);
}
