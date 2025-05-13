using System.Text;
using AuthService.Applications.Services;
using AuthService.Infraestructure.Services;
using AuthService.Middleware;
using Common;
using Infraestructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SharedLibrary;
using TAXPRO.SharedLibrary;


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

try
{
    Log.Information("Starting up the application");
    // Configurar CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Configurar Swagger (nativo de .NET 9)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service API", Version = "v1" });

    // Configuración para utilizar JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

    // Configurar JWT
    JwtSettings jwtSetting = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSetting.SecretKey);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = jwtSetting.ValidateIssuer,
            ValidateAudience = jwtSetting.ValidateAudience,
            ValidateLifetime = jwtSetting.ValidateLifetime,
            ValidateIssuerSigningKey = jwtSetting.ValidateIssuerSigningKey,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = jwtSetting.ClockSkew,
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<IPasswordHash, PasswordHash>();
    builder.Services.AddScoped<ITokenService, TokenService>();

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

    var connectionString = $"Server={objetoConexion.Server};Database=AuthDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    var app = builder.Build();

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
        opt.RoutePrefix = "swagger";        // =>  http://localhost:5092/swagger
    });


    // HTTPS redirection (opcional, solo si configuras HTTPS en Docker)
    app.UseHttpsRedirection();

    app.UseSessionValidation();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RestrictAccessMiddleware>();
    app.MapControllers();

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