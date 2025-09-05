using Ocelot.DependencyInjection;

namespace ApiGateway;

internal static class OcelotConfigTransform
{
    public static IConfigurationBuilder AddPatchedOcelot(
        this IConfigurationBuilder builder,
        IWebHostEnvironment env
    )
    {
        // 1) Carga los *.json tal cual
        builder.AddOcelot(env);

        // 2) Solo parcha cuando corre dentro del contenedor
        if (Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") != "true")
            return builder;

        // 3) Construye una raíz para leer lo ya cargado
        var cfg = builder.Build();
        var routes = cfg.GetSection("Routes").GetChildren().ToList();

        // 4) Colección con overrides que inyectaremos
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < routes.Count; i++)
        {
            // Ruta: Routes:{i}:DownstreamHostAndPorts:0:Port
            string portPath = $"Routes:{i}:DownstreamHostAndPorts:0:Port";
            string hostPath = $"Routes:{i}:DownstreamHostAndPorts:0:Host";
            string pathTemplate = cfg[$"Routes:{i}:DownstreamPathTemplate"] ?? "";
            string upstreamPath = cfg[$"Routes:{i}:UpstreamPathTemplate"] ?? "";

            if (!int.TryParse(cfg[portPath], out int port))
                continue;

            // **CONFIGURACIÓN ESPECIAL PARA WEBSOCKETS**
            if (
                pathTemplate.Contains("/ws", StringComparison.OrdinalIgnoreCase)
                || upstreamPath.Contains("/ws", StringComparison.OrdinalIgnoreCase)
            )
            {
                Console.WriteLine(
                    $"[WEBSOCKET] Configurando ruta WebSocket: {pathTemplate} -> {upstreamPath}"
                );

                // Para WebSocket, usar HTTP (no WS) porque el upgrade se maneja en ASP.NET Core
                overrides[$"Routes:{i}:DownstreamScheme"] = "http";

                // Headers específicos para WebSocket
                overrides[$"Routes:{i}:AddHeadersToDownstream:Connection"] = "Upgrade";
                overrides[$"Routes:{i}:AddHeadersToDownstream:Upgrade"] = "websocket";
                overrides[$"Routes:{i}:AddHeadersToDownstream:Sec-WebSocket-Version"] = ">>";
                overrides[$"Routes:{i}:AddHeadersToDownstream:Sec-WebSocket-Key"] = ">>";
                overrides[$"Routes:{i}:AddHeadersToDownstream:Sec-WebSocket-Protocol"] = ">>";

                // Headers de respuesta WebSocket
                overrides[$"Routes:{i}:AddHeadersToUpstream:Sec-WebSocket-Accept"] = ">>";
                overrides[$"Routes:{i}:AddHeadersToUpstream:Sec-WebSocket-Protocol"] = ">>";

                // QoS específico para WebSocket
                overrides[$"Routes:{i}:QoSOptions:TimeoutValue"] = "60000"; // 60 segundos
                overrides[$"Routes:{i}:QoSOptions:ExceptionsAllowedBeforeBreaking"] = "5";
                overrides[$"Routes:{i}:QoSOptions:DurationOfBreak"] = "5000"; // 5 segundos

                // Rate limiting más permisivo para WebSocket
                overrides[$"Routes:{i}:RateLimitOptions:Limit"] = "100";
                overrides[$"Routes:{i}:RateLimitOptions:Period"] = "1m";
                overrides[$"Routes:{i}:RateLimitOptions:PeriodTimespan"] = "60";

                // HTTP Handler options para WebSocket
                overrides[$"Routes:{i}:HttpHandlerOptions:AllowAutoRedirect"] = "false";
                overrides[$"Routes:{i}:HttpHandlerOptions:UseProxy"] = "false";
                overrides[$"Routes:{i}:HttpHandlerOptions:UseCookieContainer"] = "false";
                overrides[$"Routes:{i}:HttpHandlerOptions:MaxConnectionsPerServer"] = "200";
            }

            // Mapeo estándar de puertos a servicios - USAR LA CLASE EXISTENTE
            if (!ServiceMap.Map.TryGetValue(port, out var repl))
                continue;

            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Host"] = repl.host;
            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Port"] = repl.port.ToString();

            Console.WriteLine(
                $"[CONFIG] Mapeando {port} -> {repl.host}:{repl.port} para ruta {pathTemplate}"
            );
        }

        // **CONFIGURACIONES GLOBALES ESPECÍFICAS PARA WEBSOCKET**

        // Headers globales que se deben preservar para WebSocket
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Connection"] = ">>";
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Upgrade"] = ">>";
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Sec-WebSocket-Key"] = ">>";
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Sec-WebSocket-Version"] = ">>";
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Sec-WebSocket-Protocol"] = ">>";
        overrides["GlobalConfiguration:DownstreamHeaderTransform:Sec-WebSocket-Extensions"] = ">>";

        // Headers de respuesta WebSocket
        overrides["GlobalConfiguration:UpstreamHeaderTransform:Sec-WebSocket-Accept"] = ">>";
        overrides["GlobalConfiguration:UpstreamHeaderTransform:Sec-WebSocket-Protocol"] = ">>";

        // HTTP Handler global optimizado para WebSocket
        overrides["GlobalConfiguration:HttpHandlerOptions:MaxConnectionsPerServer"] = "512";
        overrides["GlobalConfiguration:HttpHandlerOptions:AllowAutoRedirect"] = "false";
        overrides["GlobalConfiguration:HttpHandlerOptions:UseProxy"] = "false";
        overrides["GlobalConfiguration:HttpHandlerOptions:UseCookieContainer"] = "false";

        // QoS global
        overrides["GlobalConfiguration:QoSOptions:TimeoutValue"] = "60000";
        overrides["GlobalConfiguration:QoSOptions:ExceptionsAllowedBeforeBreaking"] = "5";
        overrides["GlobalConfiguration:QoSOptions:DurationOfBreak"] = "10000";

        // 5) Añadimos los valores parcheados
        builder.AddInMemoryCollection(overrides);

        Console.WriteLine(
            $"[OCELOT] Configuración parcheada aplicada. Total overrides: {overrides.Count}"
        );

        return builder;
    }
}
