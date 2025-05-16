using Microsoft.AspNetCore.Http;

namespace ApiGateway.Middlewares;

public class TokenCheckerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string requestPath = context.Request.Path.Value!;
        if (requestPath.Contains("auth/login", StringComparison.InvariantCultureIgnoreCase) ||
            requestPath.Contains("taxuser/register", StringComparison.InvariantCultureIgnoreCase) || 
            requestPath.Contains("Session/Login", StringComparison.InvariantCultureIgnoreCase) || 
            requestPath.Contains("TaxUser/Create", StringComparison.InvariantCultureIgnoreCase) ||
            requestPath.Equals("/"))
        {
            await next(context);
        }
        else
        {
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader.FirstOrDefault() == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Sorry, Access Denied");
            }
            else
            {
                await next(context);
            }
        }
    }
}
