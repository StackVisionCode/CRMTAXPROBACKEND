using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Security;
using CommLinkService.Infrastructure.Services;
using CommLinkService.Infrastructure.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configurar logs con Serilog
var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        Path.Combine(logFolderPath, "LogsApplication-.txt"),
        rollingInterval: RollingInterval.Day
    )
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting CommLink Service");

    // CONFIGURAR CACHÉ HÍBRIDO (OBLIGATORIO)
    builder.Services.AddHybridCache(builder.Configuration);

    // JWT Authentication
    builder.Services.AddJwtAuth(builder.Configuration);

    builder
        .Services.AddAuthentication("Bearer")
        .AddJwtBearer(
            "Bearer",
            opts =>
            {
                var cfg = builder.Configuration.GetSection("JwtSettings");
                opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);

                // Support WebSocket authentication via query string
                opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            }
        );

    // Add services to the container.
    builder.Services.AddCustomCors();

    // HEALTH CHECKS
    builder.Services.AddCacheHealthChecks();

    // HttpClient for Auth Service
    builder.Services.AddHttpClient(
        "Auth",
        c =>
        {
            c.BaseAddress = new Uri(
                builder.Configuration["Services:Auth"] ?? "http://localhost:5001"
            );
            c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
        }
    );

    // Add services Origin URL to the container.
    builder.Services.AddCustomOrigin();

    builder.Services.AddEventBus(builder.Configuration);

    // Add services to the container.
    builder.Services.AddAuthorization();

    // WebSocket Manager
    builder.Services.AddSingleton<IWebSocketManager, AppWebSocketManager>();

    // Security
    builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();

    // Add services
    builder.Services.AddControllers();

    //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });

    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CommLinkService API", Version = "v1" });

        // Configuración de JWT para Swagger
        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
            }
        );

        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            }
        );
    });

    // Database
    var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=CommLinkDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<CommLinkDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    builder.Services.AddScoped<ICommLinkDbContext>(provider =>
        provider.GetRequiredService<CommLinkDbContext>()
    );

    // RBAC
    //builder.Services.AddRbac(builder.Configuration);

    var app = builder.Build();

    // ✅ 5. MOSTRAR INFORMACIÓN DEL CACHÉ
    using (var scope = app.Services.CreateScope())
    {
        var hybridCache = scope.ServiceProvider.GetService<SharedLibrary.Caching.IHybridCache>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (hybridCache != null)
        {
            logger.LogInformation(
                "✅ CommLink Service Cache initialized - Mode: {CacheMode}, Redis Available: {RedisAvailable}",
                hybridCache.CurrentCacheMode,
                hybridCache.IsRedisAvailable
            );
        }
    }

    app.UseCors("AllowAll");

    // WebSocket support
    var webSocketOptions = new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(30),
        ReceiveBufferSize = 4 * 1024,
    };
    app.UseWebSockets(webSocketOptions);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseSessionValidation();
    app.UseAuthorization();

    // WebSocket middleware
    app.UseMiddleware<WebSocketMiddleware>();

    // Gateway validation
    app.UseMiddleware<RequireGatewayHeaderMiddleware>();

    // HEALTH ENDPOINT
    app.MapHealthChecks("/health");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CommLink Service failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
