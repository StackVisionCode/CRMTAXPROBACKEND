using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using ApiGateway;
using ApiGateway.Applications.Handlers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
    Log.Information("Starting up API Gateway with WebSocket support");

    // CONFIGURAR KESTREL PARA SSL
    builder.WebHost.ConfigureKestrel(
        (context, serverOptions) =>
        {
            var runningInDocker = Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") == "true";

            if (runningInDocker)
            {
                // En producción Docker: HTTP y HTTPS
                serverOptions.Listen(
                    IPAddress.Any,
                    80,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    }
                );

                serverOptions.Listen(
                    IPAddress.Any,
                    443,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            // Certificado auto-firmado para desarrollo
                            // En producción reemplazar con certificado válido
                            httpsOptions.ServerCertificate = CreateSelfSignedCertificate();
                        });
                    }
                );
            }
            else
            {
                // Desarrollo: solo HTTP
                serverOptions.Listen(IPAddress.Any, 80);
            }

            // Configuraciones para WebSocket
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            serverOptions.AddServerHeader = false;
        }
    );

    // CONFIGURAR CACHÉ HÍBRIDO
    builder.Services.AddHybridCache(builder.Configuration);

    builder.Services.Configure<MemoryCacheOptions>(opts => opts.SizeLimit = null);

    builder.Services.AddTransient<GatewayHeaderHandler>();

    // Add Ocelot
    builder
        .Services.AddOcelot(builder.Configuration)
        .AddDelegatingHandler<GatewayHeaderHandler>(true);

    // Add CORS mejorado para WebSockets
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowWebSockets",
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
                    .SetIsOriginAllowed(_ => true);
            }
        );
    });

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
                // Soporte completo para WebSocket authentication
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
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/ws"))
                        {
                            Log.Warning(
                                "WebSocket authentication failed: {Error}",
                                context.Exception.Message
                            );
                        }
                        return Task.CompletedTask;
                    },
                };
            }
        );

    // HttpClient para auth service
    builder.Services.AddHttpClient(
        "Auth",
        c =>
        {
            var runningInDocker = Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") == "true";
            var baseUrl = runningInDocker ? "http://auth-service:8080" : "http://localhost:5001";

            c.BaseAddress = new Uri(baseUrl);
            c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
            c.Timeout = TimeSpan.FromSeconds(30);
        }
    );

    // CONFIGURAR HEALTH CHECKS
    builder.Services.AddCacheHealthChecks();

    var app = builder.Build();

    // CONFIGURAR PROXY HEADERS PARA CLOUDFLARE/PROXIES - CORREGIDO
    var fwd = new ForwardedHeadersOptions
    {
        // CORREGIDO: Remover XForwardedPort que no existe
        ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost,
    };
    fwd.KnownNetworks.Clear();
    fwd.KnownProxies.Clear();

    // Cloudflare IP ranges (principales) - CORREGIDO: Usar el tipo correcto
    var cloudflareIps = new[]
    {
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",
    };

    foreach (var ip in cloudflareIps)
    {
        try
        {
            // CORREGIDO: Usar Microsoft.AspNetCore.HttpOverrides.IPNetwork
            var network = Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse(ip);
            fwd.KnownNetworks.Add(network);
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to parse Cloudflare IP range {IpRange}: {Error}", ip, ex.Message);
        }
    }

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

    app.UseCors("AllowWebSockets");

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    // WEBSOCKET CONFIGURATION - ANTES DE OCELOT
    var webSocketOptions = new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(30),
        AllowedOrigins = { "*" }, // Ajustar según necesidades de seguridad
    };
    app.UseWebSockets(webSocketOptions);

    // Custom WebSocket middleware para logging y debugging
    app.Use(
        async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                Log.Information(
                    "WebSocket request detected: {Path} from {RemoteIp}",
                    context.Request.Path,
                    context.Connection.RemoteIpAddress
                );

                // Asegurar headers correctos para WebSocket
                context.Response.Headers.Append("Upgrade", "websocket");
                context.Response.Headers.Append("Connection", "Upgrade");
            }

            await next();
        }
    );

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
                        version = "2.0-websocket-enabled",
                        ssl_enabled = context.Request.IsHttps,
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

    // REDIRECT HTTP a HTTPS solo en producción
    var runningInDocker = Environment.GetEnvironmentVariable("RUNNING_IN_DOCKER") == "true";
    if (runningInDocker)
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseSessionValidation();
    app.UseAuthorization();

    // Use Ocelot middleware - DEBE IR AL FINAL
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

// CORREGIDO: Helper para crear certificado auto-firmado usando X509CertificateLoader
static X509Certificate2 CreateSelfSignedCertificate()
{
    var sanBuilder = new SubjectAlternativeNameBuilder();
    sanBuilder.AddDnsName("api.taxprosuite.com");
    sanBuilder.AddDnsName("localhost");
    sanBuilder.AddIpAddress(IPAddress.Loopback);
    sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);

    var distinguishedName = new X500DistinguishedName("CN=api.taxprosuite.com");

    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        distinguishedName,
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1
    );

    request.CertificateExtensions.Add(sanBuilder.Build());
    request.CertificateExtensions.Add(
        new X509KeyUsageExtension(
            X509KeyUsageFlags.DataEncipherment
                | X509KeyUsageFlags.KeyEncipherment
                | X509KeyUsageFlags.DigitalSignature,
            false
        )
    );
    request.CertificateExtensions.Add(
        new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)
    );

    var certificate = request.CreateSelfSigned(
        new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
        new DateTimeOffset(DateTime.UtcNow.AddDays(365))
    );

    // CORREGIDO: Usar X509CertificateLoader en lugar del constructor obsoleto
    var pfxBytes = certificate.Export(X509ContentType.Pfx, "");
    return X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.MachineKeySet);
}
