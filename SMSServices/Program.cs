using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedLibrary;
using SMSServices.Application.Models;
using SMSServices.Infrastructure.Context;
using SMSServices.Infrastructure.Repositories;
using SMSServices.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configurar y validar Twilio settings con logging detallado
var twilioSection = builder.Configuration.GetSection("Twilio");
var twilioSettings = twilioSection.Get<TwilioSettings>();

// Log de debugging para ver valores de configuración
Console.WriteLine("=== DEBUGGING TWILIO CONFIG ===");
Console.WriteLine($"AccountSid: '{twilioSettings?.AccountSid ?? "NULL"}'");
Console.WriteLine($"AuthToken: '{(string.IsNullOrEmpty(twilioSettings?.AuthToken) ? "NULL/EMPTY" : "CONFIGURADO")}'");
Console.WriteLine($"PhoneNumber: '{twilioSettings?.PhoneNumber ?? "NULL"}'");
Console.WriteLine($"WebhookUrl: '{twilioSettings?.WebhookUrl ?? "NULL"}'");
Console.WriteLine("==============================");

// Validar configuración
if (string.IsNullOrEmpty(twilioSettings?.AccountSid))
    throw new InvalidOperationException("Twilio AccountSid no configurado en appsettings.json");

builder.Services.Configure<TwilioSettings>(twilioSection);

// Connection string desde tu SharedLibrary.ConnectionApp
var conn = new ConnectionApp();
var connectionString =
    $"Server={conn.Server};Database=SmsServiceDb;User Id={conn.User};Password={conn.Password};TrustServerCertificate=True;";

// SOLO UN REGISTRO DE DbContext (eliminar duplicación)
builder.Services.AddDbContext<SmsDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    
    // Solo para desarrollo - logs detallados
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Registrar servicios
builder.Services.AddScoped<ISmsService, SmsService>();

// Health Checks para la base de datos
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SmsDbContext>("database");

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SMS Services API",
        Version = "v1",
        Description = "API for SMS messaging services using Twilio"
    });
});

// Configurar CORS si es necesario
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Ejecutar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SmsDbContext>();
    
    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Migraciones aplicadas exitosamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al aplicar migraciones");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SMS Services API v1");
        c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// Endpoint de salud detallado
app.MapGet("/health/detailed", async (IServiceProvider services) =>
{
    var context = services.GetRequiredService<SmsDbContext>();
    
    try
    {
        await context.Database.CanConnectAsync();
        var totalMessages = await context.SmsMessages.CountAsync();
        
        return Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = app.Environment.EnvironmentName,
            Database = "Connected",
            TotalMessages = totalMessages
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            Status = "Unhealthy",
            Timestamp = DateTime.UtcNow,
            Database = "Disconnected",
            Error = ex.Message
        }, statusCode: 503);
    }
});

// Endpoint temporal para debugging (REMOVER EN PRODUCCIÓN)
app.MapGet("/debug/config", (IConfiguration config) =>
{
    var twilioSection = config.GetSection("Twilio");
    
    return Results.Json(new
    {
        TwilioSectionExists = twilioSection.Exists(),
        AccountSid = twilioSection["AccountSid"] ?? "NULL",
        AuthToken = string.IsNullOrEmpty(twilioSection["AuthToken"]) ? "NULL/EMPTY" : "CONFIGURADO",
        PhoneNumber = twilioSection["PhoneNumber"] ?? "NULL",
        WebhookUrl = twilioSection["WebhookUrl"] ?? "NULL"
    }, new JsonSerializerOptions { WriteIndented = true });
});

app.Run();