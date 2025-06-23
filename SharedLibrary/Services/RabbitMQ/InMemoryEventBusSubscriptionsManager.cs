using SharedLibrary.DTOs;

namespace SharedLibrary.Services.RabbitMQ;

/// Gestiona qué handlers están suscritos a cada evento.
public sealed class InMemoryEventBusSubscriptionsManager
{
    private readonly Dictionary<string, List<Type>> _handlers = [];

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public void AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class
    {
        var key = typeof(TEvent).Name;
        var handlerType = typeof(THandler);

        if (!_handlers.TryGetValue(key, out var handlers))
        {
            handlers = [];
            _handlers.Add(key, handlers);
        }

        if (handlers.Any(h => h == handlerType))
            throw new ArgumentException($"Handler {handlerType.Name} ya registrado para {key}");

        handlers.Add(handlerType);
    }

    public void RemoveSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class
    {
        var key = typeof(TEvent).Name;
        var handlerType = typeof(THandler);

        if (!_handlers.TryGetValue(key, out var list))
            return;

        list.Remove(handlerType);

        if (list.Count == 0)
            _handlers.Remove(key);
    }

    public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handlers[eventName];

    public string GetEventKey<T>() => typeof(T).Name;

    public Type? GetEventTypeByName(string eventName)
    {
        // 1) Intenta encontrar ya cargado en cualquier ensamblado
        return AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == eventName);
    }
}
