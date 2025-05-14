using Microsoft.EntityFrameworkCore;
using Infraestructure.Context;
using Microsoft.OpenApi.Models;
using Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);
var objetoConexion = new ConnectionApp();
    var connectionString = $"Server={objetoConexion.Server};Database=DbCompany;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
// Add services to the container.
builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseSqlServer(connectionString));

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
        Type = SecuritySchemeType.ApiKey,
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

    // Middleware de autenticación

    app.UseAuthorization();
  
    app.UseMiddleware<RestrictAccessMiddleware>();
    app.MapControllers();

    app.Run();