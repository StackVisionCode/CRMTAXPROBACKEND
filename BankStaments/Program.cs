using Application.Interfaes;
using BankStaments.Infrastructure.Services;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SharedLibrary;
using SharedLibrary.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Infrastructure Services
builder.Services.AddScoped<IFileParser, FileParser>();
//builder.Services.AddScoped<IReportGenerator, ReportGenerator>();
builder.Services.AddCustomCors();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//configure Automapper
builder.Services.AddAutoMapper(typeof(Program));

//configure MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.Lifetime = ServiceLifetime.Scoped;
    });    
  builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank Staments", Version = "v1" });
    });
    var objetoConexion = new ConnectionApp();

    var connectionString =
        $"Server={objetoConexion.Server};Database=AuthDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
    // Configurar DbContext
    builder.Services.AddDbContext<BankStamentContext>(options =>
    {
        options.UseSqlServer(connectionString);
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
