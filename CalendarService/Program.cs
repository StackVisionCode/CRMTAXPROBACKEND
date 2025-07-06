using Applications.DTO;
using Infrastructure.Commands;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddAutoMapper(typeof(Program));
   //configure mediator
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.Lifetime = ServiceLifetime.Scoped;
    });



builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
var objetoConexion = new ConnectionApp();

var connectionString =
    $"Server={objetoConexion.Server};Database=CalendarDB;User Id={objetoConexion.User};Password={objetoConexion.Password};TrustServerCertificate=True;";
// Configurar DbContext
builder.Services.AddDbContext<CalendarDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
