namespace ApiGateway;

internal static class ServiceMap
{
    // clave: puerto local    valor: nombre DNS del servicio + puerto interno
    public static readonly Dictionary<int, (string host, int port)> Map = new()
    {
        { 5001, ("auth-service", 8080) },
        { 5002, ("customer-service", 8080) },
        { 5003, ("calendar-service", 8080) },
        { 5004, ("email-service", 8080) },
        { 5005, ("bankstaments-service", 8080) },
        { 5006, ("signature-service", 8080) },
        { 5007, ("commlink-service", 8080) },
        { 5010, ("reminder-service", 8080) },
        { 5011, ("subscription-service", 8080) },
        { 5012, ("sms-service", 8080) },
        { 5014, ("landing-service", 8080) },
    };

    /// <summary>
    /// Método helper para validar configuración de WebSocket
    /// </summary>
    public static bool IsWebSocketRoute(string pathTemplate, string upstreamPath)
    {
        return pathTemplate.Contains("/ws", StringComparison.OrdinalIgnoreCase)
            || upstreamPath.Contains("/ws", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Obtener configuración específica para un servicio
    /// </summary>
    public static (string host, int port)? GetServiceConfig(int port)
    {
        return Map.TryGetValue(port, out var config) ? config : null;
    }
}
