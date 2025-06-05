using Application.Validation;
using EmailServices.Services;
using EmailServices.Services.EmailNotificationsServices;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
    )
    .Enrich.FromLogContext()
    .CreateLogger();

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
        }
    );

builder.Services.AddCustomCors();

builder.Services.AddSessionCache();

builder.Services.AddEventBus(builder.Configuration);

builder.Services.AddAuthorization();

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EmailService API", Version = "v1" });

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

var objetoConexion = new ConnectionApp();

var connectionString =
    $"Server={objetoConexion.Server};Database=EmailDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

// Configurar DbContext
builder.Services.AddDbContext<EmailContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailConfigValidator, EmailConfigValidator>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IEmailConfigProvider, EmailConfigProvider>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IEmailBuilder, EmailBuilder>();
builder.Services.AddScoped<ISmtpSender, SmtpSender>();
builder.Services.AddScoped<IEmailConfigProvider, EmailConfigProvider>();

// Configurar el contexto de eventos
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
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequireGatewayHeaderMiddleware>();
app.MapControllers();

app.Run();
