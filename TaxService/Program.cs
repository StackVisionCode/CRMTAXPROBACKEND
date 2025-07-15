using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using SharedLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
 // Configurar CORS
    builder.Services.AddCustomCors();

    // Configurar Swagger (nativo de .NET 9)
    builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TAX SERVICES", Version = "v1" });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
    // Middlewares
    app.UseCors("AllowAll");

    // Swagger UI siempre disponible
    app.UseSwaggerUI();

    app.UseSwaggerUI(opt =>
    {
        opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Services TaxCloud V1");
        opt.RoutePrefix = "swagger"; // =>  http://localhost:5092/swagger
    });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
