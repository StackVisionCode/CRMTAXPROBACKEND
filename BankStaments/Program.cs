using System.Text;
using Application.Interfaes;
using BankStaments.Infrastructure.Services;
using Infrastructure.Context;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SharedLibrary;
using SharedLibrary.Extensions;
//
// ──────────────────────────────────────────────────────────────
// 1) REGISTRAR ENCODINGS *ANTES* DE TODO
// ──────────────────────────────────────────────────────────────
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Pre-cargar alias comunes que iText suele solicitar
try
{
    _ = Encoding.GetEncoding("symbol");
    _ = Encoding.GetEncoding("standardencoding");
}
catch 
{
    Console.WriteLine("Error al registrar los encodings comunes de iText. Asegúrate de que CodePagesEncodingProvider esté registrado.");
}

//
// ──────────────────────────────────────────────────────────────
// 2) BUILDER
// ──────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Configuración para aceptar archivos grandes (hasta 50MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});
//
// ── Servicios de infraestructura
//


// Add services to the container.
// Infrastructure Services
builder.Services.AddScoped<IFileParser, FileParser>();
builder.Services.AddScoped<IStorageService, LocalFileStorageService>();

//builder.Services.AddScoped<IReportGenerator, ReportGenerator>();
builder.Services.AddCustomCors();

builder.Services.AddControllers();
builder.Services.AddHttpClient(); 
//
// ── Swagger / OpenAPI
//

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//
// ── AutoMapper
//
//configure Automapper
builder.Services.AddAutoMapper(typeof(Program));

//
// ── MediatR
//
//configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.Lifetime = ServiceLifetime.Scoped;
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank Staments", Version = "v1" });
});

builder.Services.AddQuartzShared();


//
// ── Cadena de conexión y DbContext
//
var objetoConexion = new ConnectionApp();

var connectionString =
    $"Server={objetoConexion.Server};Database=BankStamentsDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";

// Configurar DbContext
builder.Services.AddDbContext<BankStamentContext>(options =>
{
    options.UseSqlServer(connectionString);
});


//
// ──────────────────────────────────────────────────────────────
// 3) APP PIPELINE
// ──────────────────────────────────────────────────────────────
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
