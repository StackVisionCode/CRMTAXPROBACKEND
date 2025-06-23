using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Contracts;
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

    // Add token services
    builder.Services.AddJwtAuth(builder.Configuration);

    // Add services to the container.
    builder.Services.AddCustomCors();

    // Add services Origin URL to the container.
    builder.Services.AddCustomOrigin();

    builder.Services.AddEventBus(builder.Configuration);

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
    var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=RealTimeDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

    builder.WebHost.ConfigureKestrel(k =>
    {
        k.ListenAnyIP(5007); // http
        k.ListenAnyIP(7087, o => o.UseHttps()); // https
    });

    // Configurar DbContext
    builder.Services.AddDbContext<CommLinkDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "RealTime API", Version = "v1" });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseCors("AllowAll");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseHttpsRedirection();

    app.UseAuthorization();

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
