using System.Collections.Concurrent;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class RabbitMQConnectionState
{
    private volatile bool _isConnected = false;
    private volatile bool _isConnecting = false;
    private readonly object _stateLock = new();
    private readonly ConcurrentQueue<PendingEvent> _pendingEvents = new();
    private const int MAX_PENDING_EVENTS = 1000;

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            lock (_stateLock)
            {
                _isConnected = value;
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            lock (_stateLock)
            {
                _isConnecting = value;
            }
        }
    }

    public void EnqueuePendingEvent(object eventObj, string eventName)
    {
        if (_pendingEvents.Count >= MAX_PENDING_EVENTS)
        {
            // Remover eventos antiguos si hay demasiados
            _pendingEvents.TryDequeue(out _);
        }

        _pendingEvents.Enqueue(
            new PendingEvent
            {
                Event = eventObj,
                EventName = eventName,
                EnqueuedAt = DateTime.UtcNow,
            }
        );
    }

    public IEnumerable<PendingEvent> GetAndClearPendingEvents()
    {
        var events = new List<PendingEvent>();
        while (_pendingEvents.TryDequeue(out var pendingEvent))
        {
            // Solo procesar eventos de los últimos 10 minutos
            if (DateTime.UtcNow - pendingEvent.EnqueuedAt < TimeSpan.FromMinutes(10))
            {
                events.Add(pendingEvent);
            }
        }
        return events;
    }

    public int PendingEventsCount => _pendingEvents.Count;

    public sealed class PendingEvent
    {
        public object Event { get; set; } = null!;
        public string EventName { get; set; } = null!;
        public DateTime EnqueuedAt { get; set; }
    }
}
