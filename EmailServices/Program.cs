using Application.Validation;
using EmailServices.Handlers.EventsHandler;
using EmailServices.Handlers.PasswordEventsHandler;
using EmailServices.Services;
using EmailServices.Services.EmailNotificationsServices;
using Handlers.EventHandlers.InvitationEventHandlers;
using Handlers.EventHandlers.LandingEventHandlers;
using Handlers.EventHandlers.ReminderEventsHandlers;
using Handlers.EventHandlers.SignatureEventHandlers;
using Handlers.EventHandlers.UserEventHandlers;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
using SharedLibrary.DTOs.AuthEvents;
using SharedLibrary.DTOs.CustomerEventsDTO;
using SharedLibrary.DTOs.InvitationEvents;
using SharedLibrary.DTOs.ReminderEvents;
using SharedLibrary.DTOs.SignatureEvents;
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

// CONFIGURAR CACHÉ HÍBRIDO (OBLIGATORIO)
builder.Services.AddHybridCache(builder.Configuration);

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

// HEALTH CHECKS
builder.Services.AddCacheHealthChecks();

builder.Services.AddCustomCors();

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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSyncService, EmailSyncService>();
builder.Services.AddScoped<IReactiveEmailReceivingService, ReactiveEmailReceivingService>();
builder.Services.AddHostedService<ReactiveEmailBackgroundService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

// SERVICIOS ADICIONALES
builder.Services.AddScoped<IEmailStatisticsService, EmailStatisticsService>();

// Configurar el contexto de eventos
builder.Services.AddScoped<IIntegrationEventHandler<UserLoginEvent>, UserLoginEventsHandler>();
builder.Services.AddScoped<
    IIntegrationEventHandler<PasswordResetLinkEvent>,
    PasswordResetEventHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<PasswordResetOtpEvent>,
    PasswordResetOtpEventsHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<PasswordChangedEvent>,
    PasswordChangedEventHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<AccountConfirmationLinkEvent>,
    AccountConfirmationLinkHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<AccountConfirmationLinkEvent>,
    EmailConfirmationLinkHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<UserInvitationSentEvent>,
    UserInvitationSentHandler
>();
builder.Services.AddScoped<IIntegrationEventHandler<UserRegisteredEvent>, UserRegisteredHandler>();
builder.Services.AddScoped<
    IIntegrationEventHandler<AccountConfirmedEvent>,
    AccountActivatedHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<AccountConfirmedEvent>,
    LandingActivatedEmailHandlers
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<SignatureInvitationEvent>,
    SignatureInvitationHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<DocumentPartiallySignedEvent>,
    PartiallySignedHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<CustomerLoginEnabledEvent>,
    CustomerLoginEnabledEventHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<CustomerLoginDisabledEvent>,
    CustomerLoginDisabledEventHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<SecureDownloadSignedDocument>,
    SecureDocumentDownloadHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<SignatureRequestRejectedEvent>,
    SignatureRequestRejectedHandler
>();
builder.Services.AddScoped<
    IIntegrationEventHandler<ReminderDueEvent>,
    ReminderDueEventsHandler
>();
builder.Services.AddScoped<ReminderDueEventsHandler>();
builder.Services.AddScoped<UserLoginEventsHandler>();
builder.Services.AddScoped<PasswordResetEventHandler>();
builder.Services.AddScoped<PasswordResetOtpEventsHandler>();
builder.Services.AddScoped<PasswordChangedEventHandler>();
builder.Services.AddScoped<AccountConfirmationLinkHandler>();
builder.Services.AddScoped<EmailConfirmationLinkHandler>();
builder.Services.AddScoped<UserInvitationSentHandler>();
builder.Services.AddScoped<UserRegisteredHandler>();
builder.Services.AddScoped<AccountActivatedHandler>();
builder.Services.AddScoped<SignatureInvitationHandler>();
builder.Services.AddScoped<LandingActivatedEmailHandlers>();
builder.Services.AddScoped<PartiallySignedHandler>();
builder.Services.AddScoped<SignatureRequestRejectedHandler>();
builder.Services.AddScoped<SecureDocumentDownloadHandler>();
builder.Services.AddScoped<CustomerLoginEnabledEventHandler>();
builder.Services.AddScoped<CustomerLoginDisabledEventHandler>();

var app = builder.Build();

// MOSTRAR INFORMACIÓN DEL CACHÉ
using (var scope = app.Services.CreateScope())
{
    var hybridCache = scope.ServiceProvider.GetService<SharedLibrary.Caching.IHybridCache>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (hybridCache != null)
    {
        logger.LogInformation(
            "Email Service Cache initialized - Mode: {CacheMode}, Redis Available: {RedisAvailable}",
            hybridCache.CurrentCacheMode,
            hybridCache.IsRedisAvailable
        );
    }
}

using (var scope = app.Services.CreateScope())
{
    var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
    bus.Subscribe<UserLoginEvent, UserLoginEventsHandler>();
    bus.Subscribe<PasswordResetLinkEvent, PasswordResetEventHandler>();
    bus.Subscribe<PasswordResetOtpEvent, PasswordResetOtpEventsHandler>();
    bus.Subscribe<PasswordChangedEvent, PasswordChangedEventHandler>();
    bus.Subscribe<AccountConfirmationLinkEvent, AccountConfirmationLinkHandler>();
    bus.Subscribe<UserInvitationSentEvent, UserInvitationSentHandler>();
    bus.Subscribe<UserRegisteredEvent, UserRegisteredHandler>();
    bus.Subscribe<AccountConfirmedEvent, AccountActivatedHandler>();
    bus.Subscribe<SignatureInvitationEvent, SignatureInvitationHandler>();
    bus.Subscribe<SignatureRequestRejectedEvent, SignatureRequestRejectedHandler>();
    bus.Subscribe<DocumentPartiallySignedEvent, PartiallySignedHandler>();
    bus.Subscribe<SecureDownloadSignedDocument, SecureDocumentDownloadHandler>();
    bus.Subscribe<CustomerLoginEnabledEvent, CustomerLoginEnabledEventHandler>();
    bus.Subscribe<CustomerLoginDisabledEvent, CustomerLoginDisabledEventHandler>();
    bus.Subscribe<ReminderDueEvent, ReminderDueEventsHandler>();
   bus.Subscribe<AccountConfirmationLinkEvent,EmailConfirmationLinkHandler>();
   bus.Subscribe<AccountConfirmedEvent,LandingActivatedEmailHandlers>();

    // Log successful subscriptions
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("EmailService subscribed to all integration events");
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

// app.UseMiddleware<RequireGatewayHeaderMiddleware>();

// HEALTH ENDPOINT
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
