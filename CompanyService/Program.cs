using CompanyService.Application.Handlers.IntegrationEvents;
using Infraestructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SharedLibrary;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);
var objetoConexion = new ConnectionApp();
var connectionString =
    $"Server={objetoConexion.Server};Database=CompanyDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

// Add services to the container.
builder.Services.AddDbContext<CompanyDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registrar AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

//configure mediator
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.Lifetime = ServiceLifetime.Scoped;
});

// Configurar CORS
builder.Services.AddCustomCors();

// Configurar Swagger (nativo de .NET 9)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Compay Service API", Version = "v1" });

    // Configuración para utilizar JWT en Swagger
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
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

// Configurar caché en memoria en lugar de Redis
builder.Services.AddSessionCache();

// // Configurar RabbitMQ
// builder.Services.AddEventBus(builder.Configuration);

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        opts =>
        {
            var cfg = builder.Configuration.GetSection("JwtSettings");
            opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
        }
    );

builder.Services.AddAuthorization();

// builder.Services.AddScoped<IIntegrationEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
// builder.Services.AddScoped<UserCreatedEventHandler>();

var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//     var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
//     bus.Subscribe<UserCreatedEvent, UserCreatedEventHandler>();
// }

// Middlewares
app.UseCors("AllowAll");

// Swagger UI siempre disponible
app.UseSwagger(opt =>
{
    opt.RouteTemplate = "openapi/{documentName}.json";
});

app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/openapi/v1.json", "Services TaxCloud V1");
    opt.RoutePrefix = "swagger"; // =>  http://localhost:5092/swagger
});

// HTTPS redirection (opcional, solo si configuras HTTPS en Docker)
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequireGatewayHeaderMiddleware>();
app.MapControllers();

app.Run();
