using System.Text.Json;
using ApiGateway;
using ApiGateway.Applications.Handlers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using SharedLibrary.Extensions;
using SharedLibrary.Logs;
using SharedLibrary.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddPatchedOcelot(builder.Environment);

// Configure Serilog
// Llama al método para configurar Serilog desde la SharedLibrary
SerilogConfiguration.ConfigureSerilog(builder.Configuration);

builder.Host.UseSerilog();

try
{
    Log.Information("Starting up API Gateway");

    // Add Serilog to ASP.NET Core
    builder.Host.UseSerilog();

    // CONFIGURAR CACHÉ HÍBRIDO
    builder.Services.AddHybridCache(builder.Configuration);

    builder.Services.Configure<MemoryCacheOptions>(opts => opts.SizeLimit = null);

    builder.Services.AddTransient<GatewayHeaderHandler>();

    // Add Ocelot
    builder
        .Services.AddOcelot(builder.Configuration)
        .AddDelegatingHandler<GatewayHeaderHandler>(true);

    // Add CORS
    builder.Services.AddCustomCors();

    // Configure JWT Authentication
    // ====== JWT & Sesiones ======
    builder.Services.AddJwtAuth(builder.Configuration); // reutiliza JwtSettings
    builder
        .Services.AddAuthentication("Bearer")
        .AddJwtBearer(
            "Bearer",
            opts =>
            {
                var cfg = builder.Configuration.GetSection("JwtSettings");
                opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
            }
        );

    // Cliente HTTP para auth service
    // HttpClient SIEMPRE a DNS interno
    builder.Services.AddHttpClient(
        "Auth",
        c =>
        {
            var runningInDocker = Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") == "true";

            var baseUrl = runningInDocker
                ? "http://auth-service:8080" // nombre DNS en la red bridge
                : "http://localhost:5001"; // desarrollo local

            c.BaseAddress = new Uri(baseUrl);
            c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
        }
    );

    // CONFIGURAR HEALTH CHECKS
    builder.Services.AddCacheHealthChecks();

    var app = builder.Build();
    var fwd = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    fwd.KnownNetworks.Clear();
    fwd.KnownProxies.Clear();
    app.UseForwardedHeaders(fwd);

    // MOSTRAR INFORMACIÓN DEL CACHÉ AL INICIAR
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var hybridCache =
                scope.ServiceProvider.GetService<SharedLibrary.Caching.IHybridCache>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            if (hybridCache != null)
            {
                logger.LogInformation(
                    "API Gateway Cache initialized - Mode: {CacheMode}, Redis Available: {RedisAvailable}",
                    hybridCache.CurrentCacheMode,
                    hybridCache.IsRedisAvailable
                );
            }
            else
            {
                logger.LogWarning("⚠️ HybridCache not available in Gateway");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not retrieve gateway cache status");
        }
    }

    app.UseCors("AllowAll");

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    // HEALTH CHECKS ENDPOINT
    app.MapHealthChecks(
        "/health",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(
                    new
                    {
                        status = report.Status.ToString(),
                        service = "ApiGateway",
                        timestamp = DateTime.UtcNow,
                        checks = report.Entries.ToDictionary(
                            kvp => kvp.Key,
                            kvp => new
                            {
                                status = kvp.Value.Status.ToString(),
                                description = kvp.Value.Description,
                                data = kvp.Value.Data,
                            }
                        ),
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await context.Response.WriteAsync(result);
            },
        }
    );

    if (Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") != "true")
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseSessionValidation();

    app.UseAuthorization();

    /* --- AÑADE ESTO --- */
    var wsOptions = new WebSocketOptions
    {
        // opcional: mantén los defaults o ajusta si quieres
        KeepAliveInterval = TimeSpan.FromSeconds(30),
    };
    app.UseWebSockets(wsOptions);

    // Use Ocelot middleware
    await app.UseOcelot();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
