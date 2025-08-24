using System.Text.Json.Serialization;
using Infrastructure.Context;
using Infrastructure.Commands; // para CreateCalendarEventCommand
using Microsoft.EntityFrameworkCore;
using SharedLibrary;
using Infrastructure.Reminders;
using Microsoft.Extensions.Options;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddReminderClient(builder.Configuration);

    // Opciones del ReminderClient (puedes mover a appsettings: "ReminderClient": {...})
builder.Services.Configure<ReminderClientOptions>(builder.Configuration.GetSection("ReminderClient"));

// HttpClient hacia el API Gateway (Ocelot)
builder.Services.AddHttpClient<IReminderClient, ReminderClient>(client =>
{
    var baseUrl = builder.Configuration["ReminderClient:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});


// AutoMapper: escanea todos los ensamblados cargados
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// MediatR: **FIX** registrar por ensamblado (no pasar Assembly[])
builder.Services.AddMediatR(cfg =>
{
    // Usa tipos ancla para que MediatR encuentre tus handlers/requests
   cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly, typeof(CreateCalendarEventCommand).Assembly);
});

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// CORS básico para Angular (ajusta orígenes si hace falta)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        p => p
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// HttpClient opcional para ReminderService
builder.Services.AddHttpClient("reminders", client =>
{
    var baseUrl = builder.Configuration["ReminderService:BaseUrl"] ?? "http://reminderservice:8080";
    client.BaseAddress = new Uri(baseUrl);
});

// Connection string desde tu SharedLibrary.ConnectionApp
var conn = new ConnectionApp();
var connectionString =
    $"Server={conn.Server};Database=CalendarDB;User Id={conn.User};Password={conn.Password};TrustServerCertificate=True;";

// DbContext
builder.Services.AddDbContext<CalendarDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
});
builder.Services.AddEventBus(builder.Configuration);
var app = builder.Build();

// ---------- Middleware ----------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Aplicar migraciones en arranque (opcional, útil en dev/docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();
