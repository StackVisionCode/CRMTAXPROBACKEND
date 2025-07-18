using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Infrastructure.WebSockets;

public sealed class AppWebSocketManager : IWebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections = new();
    private readonly ILogger<AppWebSocketManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AppWebSocketManager(
        ILogger<AppWebSocketManager> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task AddConnectionAsync(Guid userId, string connectionId, WebSocket webSocket)
    {
        var connection = new WebSocketConnection
        {
            UserId = userId,
            ConnectionId = connectionId,
            WebSocket = webSocket,
            ConnectedAt = DateTime.UtcNow,
        };

        _connections.TryAdd(connectionId, connection);

        _userConnections.AddOrUpdate(
            userId,
            new HashSet<string> { connectionId },
            (key, set) =>
            {
                set.Add(connectionId);
                return set;
            }
        );

        // Store connection in database
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

        var dbConnection = new Connection(userId, connectionId, null, null);
        context.Connections.Add(dbConnection);
        await context.SaveChangesAsync();

        _logger.LogInformation(
            "WebSocket connection {ConnectionId} added for user {UserId}",
            connectionId,
            userId
        );
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            if (_userConnections.TryGetValue(connection.UserId, out var connections))
            {
                connections.Remove(connectionId);
                if (!connections.Any())
                {
                    _userConnections.TryRemove(connection.UserId, out _);
                }
            }

            // Update database
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

            var dbConnection = await context.Connections.FirstOrDefaultAsync(c =>
                c.ConnectionId == connectionId
            );

            if (dbConnection != null)
            {
                dbConnection.Disconnect();
                await context.SaveChangesAsync();
            }

            _logger.LogInformation("WebSocket connection {ConnectionId} removed", connectionId);
        }
    }

    public async Task SendToUserAsync(Guid userId, object data)
    {
        _logger.LogInformation("Attempting to send message to user {UserId}", userId);

        if (_userConnections.TryGetValue(userId, out var connectionIds))
        {
            _logger.LogInformation(
                "Found {Count} connections for user {UserId}",
                connectionIds.Count,
                userId
            );
            var tasks = connectionIds.Select(connId => SendToConnectionAsync(connId, data));
            await Task.WhenAll(tasks);
        }
        else
        {
            _logger.LogWarning("No connections found for user {UserId}", userId);
        }
    }

    public async Task SendToConnectionAsync(string connectionId, object data)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            if (connection.WebSocket.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                var buffer = new ArraySegment<byte>(bytes);

                try
                {
                    await connection.WebSocket.SendAsync(
                        buffer,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error sending to connection {ConnectionId}",
                        connectionId
                    );
                    await RemoveConnectionAsync(connectionId);
                }
            }
        }
    }

    public async Task SendToRoomAsync(Guid roomId, object data, Guid? excludeUserId = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

        var participants = await context
            .RoomParticipants.Where(p => p.RoomId == roomId && p.IsActive)
            .Select(p => p.UserId)
            .ToListAsync();

        var tasks = participants
            .Where(userId => userId != excludeUserId)
            .Select(userId => SendToUserAsync(userId, data));

        await Task.WhenAll(tasks);
    }

    public WebSocket? GetWebSocket(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection)
            ? connection.WebSocket
            : null;
    }

    public bool IsUserOnline(Guid userId)
    {
        return _userConnections.ContainsKey(userId);
    }

    public int GetOnlineUsersCount()
    {
        return _userConnections.Count;
    }
}

internal class WebSocketConnection
{
    public Guid UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public WebSocket WebSocket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
}
