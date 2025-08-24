using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Applications.Common;
using Applications.EventHandlers.CustomerEventHandlers;
using AuthService.Applications.Services;
using AuthService.Infraestructure.Services;
using Infraestructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;
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
    Log.Information("Starting up the application");

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

    // Configurar caché hibrido Redis X Local
    builder.Services.AddHybridCache(builder.Configuration);

    // Configurar CORS
    builder.Services.AddCustomCors();

    // Configurar Swagger (nativo de .NET 9)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });

        // Configuración para utilizar JWT en Swagger
        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme.",
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
                    new string[] { }
                },
            }
        );
    });

    // Configurar Rbac
    builder.Services.AddRbac(builder.Configuration);

    // Configurar JWT
    builder.Services.AddJwtAuth(builder.Configuration);

    builder.Services.AddHttpClient(
        "Customers",
        c =>
        {
            var customerServiceUrl = builder.Configuration["Services:Customer"];
            if (string.IsNullOrWhiteSpace(customerServiceUrl))
            {
                throw new InvalidOperationException(
                    "Customer service URL is not configured. Please set 'Services:Customer' in your configuration."
                );
            }
            c.BaseAddress = new Uri(customerServiceUrl); // http://localhost:5002
            c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
        }
    );

    // CONFIGURAR HEALTH CHECKS
    builder.Services.AddCacheHealthChecks();

    // CONFIGURAR RABBITMQ
    builder.Services.AddEventBus(builder.Configuration);

    builder
        .Services.AddAuthentication("Bearer")
        .AddJwtBearer(
            "Bearer",
            opts =>
            {
                var cfg = builder.Configuration.GetSection("JwtSettings");
                opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
                opts.MapInboundClaims = false;
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            }
        );

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<IPasswordHash, PasswordHash>();
    builder.Services.AddScoped<IGeolocationService, GeolocationService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<LinkBuilder>();

    builder.Services.AddControllers();

    // Registrar AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });

    var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=AuthDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    // Configurar el contexto de eventos
    builder.Services.AddScoped<
        IIntegrationEventHandler<CustomerRoleAssignedEvent>,
        CustomerRoleAssignedHandler
    >();

    builder.Services.AddScoped<CustomerRoleAssignedHandler>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        bus.Subscribe<CustomerRoleAssignedEvent, CustomerRoleAssignedHandler>();

        // Log successful subscriptions
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Customer subscribed to all integration events");

        // MOSTRAR INFORMACIÓN DEL SISTEMA DE CACHÉ
        try
        {
            var hybridCache =
                scope.ServiceProvider.GetService<SharedLibrary.Caching.IHybridCache>();
            if (hybridCache != null)
            {
                logger.LogInformation(
                    "Cache system initialized - Mode: {CacheMode}, Redis Available: {RedisAvailable}",
                    hybridCache.CurrentCacheMode,
                    hybridCache.IsRedisAvailable
                );
            }
            else
            {
                logger.LogWarning("⚠️ HybridCache not found, using fallback caching");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not retrieve cache status information");
        }
    }

    // Middlewares
    app.UseCors("AllowAll");

    // Swagger UI siempre disponible
    app.UseSwagger();

    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Services TaxCloud V1");
        opt.RoutePrefix = "swagger"; // =>  http://localhost:5092/swagger
    });

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
                        service = "AuthService",
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

    // HTTPS redirection (opcional, solo si configuras HTTPS en Docker)
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RequireGatewayHeaderMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>("AuthService");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
