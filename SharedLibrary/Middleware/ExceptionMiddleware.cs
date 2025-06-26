using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly string _serviceName;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, string serviceName)
    {
        _next = next;
        _logger = logger;
        _serviceName = serviceName ?? "UnknownService";
    }
   
        public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{_serviceName}] Exception caught. Path: {context.Request?.Path}");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var ErrorResponse = new
            {
                Service = _serviceName,
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred.",
                Error = ex.Message,
                Path = context.Request?.Path
            };

            await context.Response.WriteAsJsonAsync(ErrorResponse);
        }
    }
}