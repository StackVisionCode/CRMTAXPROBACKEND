using Application.Interfaces;
using Infraestructure.Repositories;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SharedLibrary;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCustomCors();
builder.Services.AddControllers();
   // Registrar AutoMapper
    builder.Services.AddAutoMapper(typeof(Program));

  //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });

builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
 var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=SignatureDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<SignatureDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
        builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Signature API", Version = "v1" });

});


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
