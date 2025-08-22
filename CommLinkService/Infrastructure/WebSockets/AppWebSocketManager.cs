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

    private readonly ConcurrentDictionary<Guid, HashSet<string>> _taxUserConnections = new();
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _customerConnections = new();

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

    public async Task AddConnectionAsync(
        ParticipantType userType,
        Guid? taxUserId,
        Guid? customerId,
        Guid? companyId,
        string connectionId,
        WebSocket webSocket,
        string? userAgent = null,
        string? ipAddress = null
    )
    {
        var connection = new WebSocketConnection
        {
            UserType = userType,
            TaxUserId = taxUserId,
            CustomerId = customerId,
            CompanyId = companyId,
            ConnectionId = connectionId,
            WebSocket = webSocket,
            ConnectedAt = DateTime.UtcNow,
        };

        _connections.TryAdd(connectionId, connection);

        if (userType == ParticipantType.TaxUser && taxUserId.HasValue)
        {
            _taxUserConnections.AddOrUpdate(
                taxUserId.Value,
                new HashSet<string> { connectionId },
                (key, set) =>
                {
                    set.Add(connectionId);
                    return set;
                }
            );
        }
        else if (userType == ParticipantType.Customer && customerId.HasValue)
        {
            _customerConnections.AddOrUpdate(
                customerId.Value,
                new HashSet<string> { connectionId },
                (key, set) =>
                {
                    set.Add(connectionId);
                    return set;
                }
            );
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

        var dbConnection = new Connection
        {
            Id = Guid.NewGuid(),
            UserType = userType,
            TaxUserId = taxUserId,
            CustomerId = customerId,
            CompanyId = companyId,
            ConnectionId = connectionId,
            ConnectedAt = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            IsActive = true,
        };

        context.Connections.Add(dbConnection);
        await context.SaveChangesAsync();

        _logger.LogInformation(
            "WebSocket connection {ConnectionId} added for {UserType} - TaxUser: {TaxUserId}, Customer: {CustomerId}",
            connectionId,
            userType,
            taxUserId,
            customerId
        );
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            if (connection.UserType == ParticipantType.TaxUser && connection.TaxUserId.HasValue)
            {
                if (
                    _taxUserConnections.TryGetValue(
                        connection.TaxUserId.Value,
                        out var taxUserConnections
                    )
                )
                {
                    taxUserConnections.Remove(connectionId);
                    if (!taxUserConnections.Any())
                    {
                        _taxUserConnections.TryRemove(connection.TaxUserId.Value, out _);
                    }
                }
            }
            else if (
                connection.UserType == ParticipantType.Customer
                && connection.CustomerId.HasValue
            )
            {
                if (
                    _customerConnections.TryGetValue(
                        connection.CustomerId.Value,
                        out var customerConnections
                    )
                )
                {
                    customerConnections.Remove(connectionId);
                    if (!customerConnections.Any())
                    {
                        _customerConnections.TryRemove(connection.CustomerId.Value, out _);
                    }
                }
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

            var dbConnection = await context.Connections.FirstOrDefaultAsync(c =>
                c.ConnectionId == connectionId
            );

            if (dbConnection != null)
            {
                dbConnection.DisconnectedAt = DateTime.UtcNow;
                dbConnection.IsActive = false;
                await context.SaveChangesAsync();
            }

            _logger.LogInformation("WebSocket connection {ConnectionId} removed", connectionId);
        }
    }

    public async Task SendToTaxUserAsync(Guid taxUserId, object data)
    {
        if (_taxUserConnections.TryGetValue(taxUserId, out var connectionIds))
        {
            var tasks = connectionIds.Select(connId => SendToConnectionAsync(connId, data));
            await Task.WhenAll(tasks);
        }
    }

    public async Task SendToCustomerAsync(Guid customerId, object data)
    {
        if (_customerConnections.TryGetValue(customerId, out var connectionIds))
        {
            var tasks = connectionIds.Select(connId => SendToConnectionAsync(connId, data));
            await Task.WhenAll(tasks);
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

    public async Task SendToRoomAsync(
        Guid roomId,
        object data,
        ParticipantType? excludeType = null,
        Guid? excludeUserId = null
    )
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommLinkDbContext>();

        var participants = await context
            .RoomParticipants.Where(p => p.RoomId == roomId && p.IsActive)
            .ToListAsync();

        var tasks = new List<Task>();

        foreach (var participant in participants)
        {
            bool shouldExclude = false;
            if (excludeType.HasValue && excludeUserId.HasValue)
            {
                if (
                    excludeType == ParticipantType.TaxUser
                    && participant.ParticipantType == ParticipantType.TaxUser
                    && participant.TaxUserId == excludeUserId
                )
                {
                    shouldExclude = true;
                }
                else if (
                    excludeType == ParticipantType.Customer
                    && participant.ParticipantType == ParticipantType.Customer
                    && participant.CustomerId == excludeUserId
                )
                {
                    shouldExclude = true;
                }
            }

            if (!shouldExclude)
            {
                if (
                    participant.ParticipantType == ParticipantType.TaxUser
                    && participant.TaxUserId.HasValue
                )
                {
                    tasks.Add(SendToTaxUserAsync(participant.TaxUserId.Value, data));
                }
                else if (
                    participant.ParticipantType == ParticipantType.Customer
                    && participant.CustomerId.HasValue
                )
                {
                    tasks.Add(SendToCustomerAsync(participant.CustomerId.Value, data));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    public WebSocket? GetWebSocket(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection)
            ? connection.WebSocket
            : null;
    }

    public bool IsTaxUserOnline(Guid taxUserId) => _taxUserConnections.ContainsKey(taxUserId);

    public bool IsCustomerOnline(Guid customerId) => _customerConnections.ContainsKey(customerId);

    public int GetOnlineUsersCount() => _taxUserConnections.Count + _customerConnections.Count;

    public IEnumerable<string> GetTaxUserConnections(Guid taxUserId)
    {
        return _taxUserConnections.TryGetValue(taxUserId, out var connections)
            ? connections.ToList()
            : Enumerable.Empty<string>();
    }

    public IEnumerable<string> GetCustomerConnections(Guid customerId)
    {
        return _customerConnections.TryGetValue(customerId, out var connections)
            ? connections.ToList()
            : Enumerable.Empty<string>();
    }
}

internal class WebSocketConnection
{
    public ParticipantType UserType { get; set; }
    public Guid? TaxUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CompanyId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public WebSocket WebSocket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
}
