using Microsoft.AspNetCore.Http;

namespace SharedLibrary;

public sealed class RequireGatewayHeaderMiddleware(RequestDelegate next)
{
    private const string HeaderName   = "X-From-Gateway";
    private const string ExpectedValue = "Api-Gateway";

    private static readonly PathString[] PublicEndpoints =
    {
        "/api/Session/Login",
        "/api/TaxUser/Create",
        "/api/Session/IsValid",
        "/api/auth/login",
        "/api/taxuser/register",
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (PublicEndpoints.Any(p =>
                ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var value) ||
            value != ExpectedValue)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Access denied - outside gateway");
            return;
        }
        await next(ctx);
    }
}