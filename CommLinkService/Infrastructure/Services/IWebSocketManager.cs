using System.Net.WebSockets;

namespace CommLinkService.Infrastructure.Services;

public interface IWebSocketManager
{
    Task AddConnectionAsync(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        string connectionId,
        WebSocket webSocket,
        string? userAgent = null,
        string? ipAddress = null
    );

    Task RemoveConnectionAsync(string connectionId);
    Task SendToTaxUserAsync(Guid taxUserId, object data);
    Task SendToCustomerAsync(Guid customerId, object data);
    Task SendToConnectionAsync(string connectionId, object data);
    Task SendToRoomAsync(
        Guid roomId,
        object data,
        ParticipantType? excludeType = null,
        Guid? excludeUserId = null
    );

    WebSocket? GetWebSocket(string connectionId);
    bool IsTaxUserOnline(Guid taxUserId);
    bool IsCustomerOnline(Guid customerId);
    int GetOnlineUsersCount();

    IEnumerable<string> GetTaxUserConnections(Guid taxUserId);
    IEnumerable<string> GetCustomerConnections(Guid customerId);
}
