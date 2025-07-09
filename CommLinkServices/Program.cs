using CommLinkServices.Application.Handlers.Integrations;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents.IdentityEvents;
using SharedLibrary.Extensions;

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

    builder.Services.AddJwtAuth(builder.Configuration);
    builder
        .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(
            "Bearer",
            opts =>
            {
                var cfg = builder.Configuration.GetSection("JwtSettings");
                opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
                opts.MapInboundClaims = false; // Mantener esto si es necesario

                // **Añadir esto para SignalR**
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (
                            !string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/realtime")
                        ) // Asegúrate de que coincida con tu ruta de SignalR
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

    // Configurar caché en memoria en lugar de Redis
    builder.Services.AddSessionCache();

    // Add services Origin URL to the container.
    builder.Services.AddCustomOrigin();

    builder.Services.AddEventBus(builder.Configuration);

    // Add services to the container.
    builder.Services.AddAuthorization();

    // Add SignalR + back-plane a RabbitMQ
    builder.Services.AddRealtimeComm<CommHub>(builder.Configuration);

    builder.Services.AddControllers();

    // Registrar AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

    //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CustomerService API", Version = "v1" });

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

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

    var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=RealTimeDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

    builder.WebHost.ConfigureKestrel(k =>
    {
        k.ListenAnyIP(5007); // http
        // k.ListenAnyIP(7087, o => o.UseHttps()); // https
    });

    // Configurar DbContext
    builder.Services.AddDbContext<CommLinkDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    // Register consumers by RabbitMQ
    builder.Services.AddTransient<UserCreatedHandler>();
    builder.Services.AddTransient<UserPresenceChangedHandler>();

    var app = builder.Build();

    // Suscribe to the events
    using (var scope = app.Services.CreateScope())
    {
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        bus.Subscribe<UserCreatedEvent, UserCreatedHandler>();
        bus.Subscribe<UserPresenceChangedEvent, UserPresenceChangedHandler>();
    }

    // Configure the HTTP request pipeline.
    app.UseCors("AllowAll");

    app.UseWebSockets();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    // app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RequireGatewayHeaderMiddleware>();
    app.MapControllers();

    app.MapHub<CommHub>("/realtime");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
