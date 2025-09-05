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
            if (!int.TryParse(cfg[portPath], out int port))
                continue;

            if (!ServiceMap.Map.TryGetValue(port, out var repl))
                continue;

            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Host"] = repl.host;
            overrides[$"Routes:{i}:DownstreamHostAndPorts:0:Port"] = repl.port.ToString();
        }

        // 5) Añadimos los valores parcheados
        builder.AddInMemoryCollection(overrides);
        return builder;
    }
}
