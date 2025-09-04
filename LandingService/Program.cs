using System.IdentityModel.Tokens.Jwt;
using LandingService.Applications.Services;
using LandingService.Infrastructure.Context;
using LandingService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

    // CONFIGURAR RABBITMQ
    builder.Services.AddEventBus(builder.Configuration);
    // Configurar CORS
    builder.Services.AddCustomCors();

    // Configurar Swagger (nativo de .NET 9)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "LandingPage Service API", Version = "v1" });

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

    // Configurar JWT
    builder.Services.AddJwtAuth(builder.Configuration);

    // CONFIGURAR HEALTH CHECKS
    builder.Services.AddCacheHealthChecks();

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
        $"Server={objetoConexion.Server};Database=LandingDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    var app = builder.Build();

    // Middlewares
    app.UseCors("AllowAll");

    // Swagger UI siempre disponible
    app.UseSwagger();

    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Landing Service V1");
        opt.RoutePrefix = "swagger";
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization(); 

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
