using Application.Validation;
using EmailServices.Services;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibrary;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
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
    ).Enrich.FromLogContext()
    .CreateLogger();

Log.Information("Starting up the application");

builder.Services.AddCustomCors();

builder.Services.AddEventBus(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailConfigValidator, EmailConfigValidator>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IEmailConfigProvider, EmailConfigProvider>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.Lifetime = ServiceLifetime.Scoped;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var objetoConexion = new ConnectionApp();

var connectionString = $"Server={objetoConexion.Server};Database=EmailDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
// Configurar DbContext
builder.Services.AddDbContext<EmailContext>(options =>
{
    options.UseSqlServer(connectionString);
});


builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IIntegrationEventHandler<UserLoginEvent>, UserLoginEventHandler>();

builder.Services.AddScoped<UserLoginEventHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
    bus.Subscribe<UserLoginEvent, UserLoginEventHandler>();
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();