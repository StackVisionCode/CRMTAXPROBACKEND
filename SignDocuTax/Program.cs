using Infraestructure.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedLibrary;
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

try
{
    Log.Information("Starting up the application");

    // Configurar CORS
    builder.Services.AddCustomCors();

    // Configurar Swagger (nativo de .NET 9)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configurar JWT
    builder.Services.AddJwtAuth(builder.Configuration);

    // Configurar caché en memoria en lugar de Redis
    builder.Services.AddSessionCache();

    // Configurar RabbitMQ
    builder.Services.AddEventBus(builder.Configuration);

    builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opts =>
    {
        var cfg = builder.Configuration.GetSection("JwtSettings");
        opts.TokenValidationParameters = JwtOptionsFactory.Build(cfg);
    });

    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<ITokenStorage, TokenStorage>();

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

    var connectionString = $"Server={objetoConexion.Server};Database=SignDocuTax;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

    var app = builder.Build();

    // Middlewares
    app.UseCors("AllowAll");

    // Swagger UI siempre disponible
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Services Sign Docu Tax V1");
        c.RoutePrefix = "swagger"; // Swagger en la raíz (http://localhost:5092/)
    });

    // HTTPS redirection (opcional, solo si configuras HTTPS en Docker)
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RequireGatewayHeaderMiddleware>();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw; // <- Agrega este throw

}
finally
{
    Log.CloseAndFlush();

}