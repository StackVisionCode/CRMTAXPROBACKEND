using AuthService.Infraestructure.Services;
using Infraestructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Middleware;

public class SessionMiddleware
{
    //this middleware is called here
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionMiddleware> _logger;

    public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, ITokenService tokenService)
    {
        var bearer = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer "))
        {
            await _next(context);
            return;
        }

        var token = bearer.Substring("Bearer ".Length);
        
        // 1) ¿el JWT firma / vida / issuer / audience es correcto?
        if (!tokenService.ValidateToken(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
            return;
        }

        // 2) podemos extraer los datos con seguridad
        var userId = tokenService.GetUserIdFromToken(token);
            //getSessionbyToken put in TokenServices
        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.TokenRequest == token && !s.IsRevoke);

        if (session is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Session revoked" });
            return;
        }

        if (session.ExpireTokenRequest < DateTime.UtcNow)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Token expired" });
            return;
        }

        // guardamos datos útiles para los controladores
        context.Items["SessionId"] = session.Id;
        context.Items["UserId"]    = userId;

        await _next(context);
    }
}

// Extensión para agregar el middleware a la pipeline
public static class SessionMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionMiddleware>();
    }
}