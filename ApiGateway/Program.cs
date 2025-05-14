using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ApiGateway.Middlewares;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text;
using SharedLibrary.Logs;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
// Llama al método para configurar Serilog desde la SharedLibrary
SerilogConfiguration.ConfigureSerilog(builder.Configuration);

builder.Host.UseSerilog();

try
{
    Log.Information("Starting up API Gateway");

    // Add Serilog to ASP.NET Core
    builder.Host.UseSerilog();

    builder.Configuration.AddOcelot(builder.Environment);

    // Add Ocelot
    builder.Services.AddOcelot(builder.Configuration);

    // Add CORS
    builder.Services.AddCustomCors();

    // Configure JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey))
    {
       Log.Error("JWT SecretKey is not configured in appsettings.json");
        throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");
    }

    var key = Encoding.UTF8.GetBytes(secretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = bool.Parse(jwtSettings["ValidateIssuer"] ?? "true"),
            ValidateAudience = bool.Parse(jwtSettings["ValidateAudience"] ?? "true"),
            ValidateLifetime = bool.Parse(jwtSettings["ValidateLifetime"] ?? "true"),
            ValidateIssuerSigningKey = bool.Parse(jwtSettings["ValidateIssuerSigningKey"] ?? "true"),
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });



    // Add HttpClient for service communication
    builder.Services.AddHttpClient();

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseSerilogRequestLogging();

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();

    app.UseMiddleware<TokenCheckerMiddleware>();
    app.UseMiddleware<InterceptionMiddleware>();
    app.UseAuthorization();

    // Use Ocelot middleware
    app.UseOcelot().Wait();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}