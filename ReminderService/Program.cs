using System.Text.Json.Serialization;
using Infrastructure.Context;
using Infrastructure.Services; // ReminderScheduler
using Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Serialization.Json; // 👈 agregado para UseJsonSerializer()
using SharedLibrary;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------

// Controllers + JSON
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (desde tu SharedLibrary)
builder.Services.AddCustomCors();

// RabbitMQ / EventBus (SharedLibrary)
builder.Services.AddEventBus(builder.Configuration);

// ---------- Conexiones vía SharedLibrary.ConnectionApp ----------
var conn = new ConnectionApp();

var reminderDbName = builder.Configuration["Databases:ReminderDb"] ?? "ReminderDb";
var quartzDbName   = builder.Configuration["Databases:QuartzDb"]   ?? "QuartzDb";

string BuildConn(string dbName) =>
    $"Server={conn.Server};Database={dbName};User Id={conn.User};Password={conn.Password};TrustServerCertificate=True;";

var efConnectionString = BuildConn(reminderDbName);
var quartzConnection   = BuildConn(quartzDbName);

// (opcional) crea la BD de Quartz si no existe
EnsureDatabaseExists(quartzConnection);

// EF Core: DB principal de recordatorios
builder.Services.AddDbContext<ReminderDbContext>(opt =>
    opt.UseSqlServer(efConnectionString, sql => sql.EnableRetryOnFailure()));

// Quartz (persistente en SQL Server)
builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "ReminderScheduler";

    q.UsePersistentStore(s =>
    {
        s.UseProperties = true;

        // Serializer requerido cuando NO es RAMJobStore
#pragma warning disable CS0618
        s.UseJsonSerializer(); // 👈 ya compila con el using de arriba
#pragma warning restore CS0618

        // Si aún no creaste las tablas QRTZ_ con el script oficial, déjalo en false
        s.PerformSchemaValidation = false;

        s.UseSqlServer(sql =>
        {
            sql.ConnectionString = quartzConnection;
            sql.TablePrefix = "QRTZ_";
        });

        s.UseClustering();
    });
});

builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

// Scheduler de alto nivel (crea jobs y triggers)
builder.Services.AddScoped<ReminderScheduler>();

var app = builder.Build();

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migrar/crear BD en arranque (útil en dev/docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReminderDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Helper para crear la BD si no existe (para QuartzDb)
static void EnsureDatabaseExists(string connectionString)
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
    var dbName = csb.InitialCatalog;
    csb.InitialCatalog = "master";

    using var conn = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
    conn.Open();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
IF DB_ID(@db) IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = 'CREATE DATABASE [' + REPLACE(@db,']',']]') + ']';
    EXEC (@sql);
END";
    cmd.Parameters.AddWithValue("@db", dbName);
    cmd.ExecuteNonQuery();
}
