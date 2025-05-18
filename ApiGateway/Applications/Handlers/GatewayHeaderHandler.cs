namespace ApiGateway.Applications.Handlers;   // cambia al namespace del proyecto

/// <summary>
/// Inyecta la cabecera 'X-From-Gateway: Api-Gateway' a todas las
/// peticiones downstream que Ocelot envía a los microservicios.
/// </summary>
public class GatewayHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-From-Gateway"))
        {
            request.Headers.Add("X-From-Gateway", "Api-Gateway");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
