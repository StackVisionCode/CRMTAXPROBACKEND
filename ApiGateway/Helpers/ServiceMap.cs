namespace ApiGateway;

internal static class ServiceMap
{
    // clave: puerto local    valor: nombre DNS del servicio + puerto interno
    public static readonly Dictionary<int, (string host, int port)> Map = new()
    {
        { 5092, ("auth-service", 8080) },
        { 5094, ("customer-service", 8080) },
        { 5259, ("company-service", 8080) },
        { 5066, ("docu-service", 8080) },
        { 5089, ("email-service", 8080) },
        // …añade todos los que uses
    };
}
