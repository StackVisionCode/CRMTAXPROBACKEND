using CustomerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
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

// Add services to the container.

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
builder.Services.AddSwaggerGen();
var objetoConexion = new ConnectionApp();

    var connectionString = $"Server={objetoConexion.Server};Database=CustomerDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
// Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });

var app = builder.Build();

// Middlewares
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
