namespace ApiGateway;

internal static class ServiceMap
{
    // clave: puerto local    valor: nombre DNS del servicio + puerto interno
    public static readonly Dictionary<int, (string host, int port)> Map = new()
    {
        { 5001, ("auth-service", 8080) },
        { 5002, ("customer-service", 8080) },
        { 5003, ("company-service", 8080) },
        { 5004, ("email-service", 8080) },
        { 5005, ("bankstaments-service", 8080) },
        { 5006, ("signature-service", 8080) },
        // …añade todos los que uses
    };
}
