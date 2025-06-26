using ApiGateway;
using ApiGateway.Applications.Handlers;
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

    // Configurar caché en memoria con opciones
    builder.Services.AddSessionCache();

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
    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();

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
