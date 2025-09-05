using System.Net;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Security;
using CommLinkService.Infrastructure.Services;
using CommLinkService.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
    Log.Information("Starting CommLink Service with WebSocket optimization");

    // **CONFIGURAR KESTREL PARA WEBSOCKET OPTIMIZADO**
    builder.WebHost.ConfigureKestrel(
        (context, serverOptions) =>
        {
            serverOptions.Listen(
                IPAddress.Any,
                8080,
                listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                }
            );

            // Configuraciones específicas para WebSocket
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            serverOptions.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB para WebSocket
            serverOptions.Limits.MaxConcurrentConnections = 1000;
            serverOptions.Limits.MaxConcurrentUpgradedConnections = 500;
            serverOptions.AddServerHeader = false;

            // Buffer sizes optimizados para WebSocket
            serverOptions.Limits.Http2.MaxStreamsPerConnection = 100;
            serverOptions.Limits.Http2.HeaderTableSize = 4096;
            serverOptions.Limits.Http2.MaxFrameSize = 16384;
        }
    );

    // CONFIGURAR CACHÉ HÍBRIDO (OBLIGATORIO)
    builder.Services.AddHybridCache(builder.Configuration);

    // JWT Authentication con soporte WebSocket optimizado
    builder.Services.AddJwtAuth(builder.Configuration);
    builder
        .Services.AddAuthentication("Bearer")
        .AddJwtBearer(
            "Bearer",
            opts =>
            {
                var cfg = builder.Configuration.GetSection("JwtSettings");
                opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);

                // **SOPORTE WEBSOCKET MEJORADO**
                opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                            Log.Debug(
                                "WebSocket authentication token detected for path: {Path}",
                                path
                            );
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/ws"))
                        {
                            Log.Warning(
                                "WebSocket authentication failed: {Error} for {Path}",
                                context.Exception.Message,
                                context.Request.Path
                            );
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/ws"))
                        {
                            Log.Debug(
                                "WebSocket token validated successfully for user: {User}",
                                context.Principal?.Identity?.Name
                            );
                        }
                        return Task.CompletedTask;
                    },
                };
            }
        );

    // CORS optimizado para WebSocket
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowWebSocket",
            policy =>
            {
                policy
                    .WithOrigins(
                        "https://go.taxprosuite.com",
                        "https://taxprosuite.com",
                        "https://www.taxprosuite.com",
                        "https://api.taxprosuite.com"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed(origin =>
                    {
                        // Log para debugging de CORS
                        Log.Debug("CORS origin check: {Origin}", origin);
                        return true; // Permitir todos en desarrollo, restringir en producción
                    });
            }
        );
    });

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
            c.Timeout = TimeSpan.FromSeconds(30);
        }
    );

    // Add services Origin URL to the container.
    builder.Services.AddCustomOrigin();
    builder.Services.AddEventBus(builder.Configuration);
    builder.Services.AddAuthorization();

    // **WEBSOCKET MANAGER CON CONFIGURACIÓN OPTIMIZADA**
    builder.Services.Configure<WebSocketOptions>(options =>
    {
        var maxConnections = builder.Configuration.GetValue<int>("WebSocket:MaxConnections", 1000);
        var keepAliveInterval = builder.Configuration.GetValue<int>(
            "WebSocket:KeepAliveInterval",
            30
        );
        var bufferSize = builder.Configuration.GetValue<int>("WebSocket:BufferSize", 8192);

        Log.Information(
            "WebSocket configuration: MaxConnections={MaxConnections}, KeepAlive={KeepAlive}s, BufferSize={BufferSize}",
            maxConnections,
            keepAliveInterval,
            bufferSize
        );
    });

    builder.Services.AddSingleton<IWebSocketManager, AppWebSocketManager>();

    // Security
    builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();

    // Add services
    builder.Services.AddControllers();

    // Registrar AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });

    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "CommLinkService API",
                Version = "v1",
                Description = "CommLink Service with WebSocket support for real-time communication",
            }
        );

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

    builder.Services.AddDbContext<CommLinkDbContext>(options =>
    {
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null
                );
            }
        );
    });

    builder.Services.AddScoped<ICommLinkDbContext>(provider =>
        provider.GetRequiredService<CommLinkDbContext>()
    );

    var app = builder.Build();

    // **MOSTRAR INFORMACIÓN DEL CACHÉ AL INICIAR**
    using (var scope = app.Services.CreateScope())
    {
        var hybridCache = scope.ServiceProvider.GetService<SharedLibrary.Caching.IHybridCache>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (hybridCache != null)
        {
            logger.LogInformation(
                "CommLink Service Cache initialized - Mode: {CacheMode}, Redis Available: {RedisAvailable}",
                hybridCache.CurrentCacheMode,
                hybridCache.IsRedisAvailable
            );
        }
    }

    app.UseCors("AllowWebSocket");

    // **WEBSOCKET CONFIGURATION - ORDEN IMPORTANTE**
    var webSocketOptions = new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("WebSocket:KeepAliveInterval", 30)
        ),
        AllowedOrigins = { "*" }, // Configurar según necesidades de seguridad
    };

    // Configurar buffer size si está disponible
    var bufferSize = builder.Configuration.GetValue<int>("WebSocket:BufferSize", 8192);
    if (bufferSize > 0)
    {
        webSocketOptions.ReceiveBufferSize = bufferSize;
    }

    app.UseWebSockets(webSocketOptions);

    // **MIDDLEWARE PERSONALIZADO PARA WEBSOCKET LOGGING**
    app.Use(
        async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var remoteIp = context.Connection.RemoteIpAddress?.ToString();
                var path = context.Request.Path;

                Log.Information(
                    "WebSocket request: {Path} from {RemoteIp} using {UserAgent}",
                    path,
                    remoteIp,
                    userAgent
                );

                // Asegurar headers correctos
                if (!context.Response.Headers.ContainsKey("Upgrade"))
                {
                    context.Response.Headers.Append("Upgrade", "websocket");
                }
                if (!context.Response.Headers.ContainsKey("Connection"))
                {
                    context.Response.Headers.Append("Connection", "Upgrade");
                }
            }

            await next();
        }
    );

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CommLinkService API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseHsts();
        // NO usar HTTPS redirect cuando está detrás de un proxy
        // app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseSessionValidation();
    app.UseAuthorization();

    // **WEBSOCKET MIDDLEWARE - DEBE IR ANTES DE GATEWAY VALIDATION**
    app.UseMiddleware<WebSocketMiddleware>();

    // Gateway validation - DESPUÉS de WebSocket middleware
    app.UseMiddleware<RequireGatewayHeaderMiddleware>();

    // HEALTH ENDPOINT MEJORADO
    app.MapHealthChecks(
        "/health",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                // Obtener estadísticas de WebSocket
                var wsManager = context.RequestServices.GetService<IWebSocketManager>();
                var onlineCount = wsManager?.GetOnlineUsersCount() ?? 0;

                var result = System.Text.Json.JsonSerializer.Serialize(
                    new
                    {
                        status = report.Status.ToString(),
                        service = "CommLinkService",
                        timestamp = DateTime.UtcNow,
                        version = "2.0-websocket-optimized",
                        websocket = new { online_users = onlineCount, enabled = true },
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
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                await context.Response.WriteAsync(result);
            },
        }
    );

    app.MapControllers();

    // **ENDPOINT DE INFORMACIÓN BÁSICA**
    app.MapGet(
        "/",
        () =>
            new
            {
                service = "CommLinkService",
                version = "2.0",
                features = new[] { "WebSocket", "Real-time messaging", "Video calls" },
                websocket_endpoint = "/ws",
                timestamp = DateTime.UtcNow,
            }
    );

    Log.Information("CommLink Service configured and starting...");
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
