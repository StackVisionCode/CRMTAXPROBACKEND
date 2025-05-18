using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using SharedLibrary.Logs;
using SharedLibrary.Extensions;
using SharedLibrary.Middleware;
using ApiGateway.Applications.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
// Llama al método para configurar Serilog desde la SharedLibrary
SerilogConfiguration.ConfigureSerilog(builder.Configuration);

builder.Host.UseSerilog();

try
{
    Log.Information("Starting up API Gateway");

    // Add Serilog to ASP.NET Core
    builder.Host.UseSerilog();

    builder.Configuration.AddOcelot(builder.Environment);

    builder.Services.AddTransient<GatewayHeaderHandler>();

    // Add Ocelot
    builder.Services.AddOcelot(builder.Configuration)
        .AddDelegatingHandler<GatewayHeaderHandler>(true);

    // Add CORS
    builder.Services.AddCustomCors();

    // Configure JWT Authentication
    // ====== JWT & Sesiones ======
    builder.Services.AddJwtAuth(builder.Configuration);          // reutiliza JwtSettings
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", opts =>
        {
            var cfg = builder.Configuration.GetSection("JwtSettings");
            opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
        });

    // Configurar caché en memoria con opciones
    builder.Services.AddSessionCache();
    
    // Cliente HTTP para auth service
    builder.Services.AddHttpClient("Auth", c => 
    {
        var baseUrl = builder.Environment.IsDevelopment()
                ? "http://localhost:5092"
                : "http://authservice";          // nombre del servicio en Docker
        c.BaseAddress = new Uri(baseUrl);
        c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
    });
    
    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseSessionValidation();  
    app.UseAuthorization();

    // Use Ocelot middleware
    app.UseOcelot().Wait();

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