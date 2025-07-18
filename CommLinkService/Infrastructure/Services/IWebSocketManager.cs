using System.Net.WebSockets;

namespace CommLinkService.Infrastructure.Services;

public interface IWebSocketManager
{
    Task AddConnectionAsync(Guid userId, string connectionId, WebSocket webSocket);
    Task RemoveConnectionAsync(string connectionId);
    Task SendToUserAsync(Guid userId, object data);
    Task SendToConnectionAsync(string connectionId, object data);
    Task SendToRoomAsync(Guid roomId, object data, Guid? excludeUserId = null);
    WebSocket? GetWebSocket(string connectionId);
    bool IsUserOnline(Guid userId);
    int GetOnlineUsersCount();
}
