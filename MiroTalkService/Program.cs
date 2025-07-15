using Microsoft.OpenApi.Models;
using MiroTalkService.Libs;
using SharedLibrary.Extensions;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<MiroTalkClient>();
builder.Services.AddCustomCors();
// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MiroTalk", Version = "v1" });
});


var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
 app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MiroTalk API v1");
    c.RoutePrefix = string.Empty;
});
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
