using Microsoft.AspNetCore.Http;

namespace SharedLibrary;

public sealed class RequireGatewayHeaderMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-From-Gateway";
    private const string ExpectedValue = "Api-Gateway";

    private static readonly PathString[] PublicEndpoints =
    {
        "/api/Session/Login",
        "/api/TaxUser/Create",
        "/api/TaxUser/CreateCompany",
        "/api/Session/IsValid",
        "/api/Password/request",
        "/api/Password/otp/send",
        "/api/Password/otp/validate",
        "/api/Password/reset",
        "/api/account/confirm",
        "/api/auth/customer/login",
        "/api/auth/login",
        "/api/taxuser/register",
        "/api/taxcompany/register",
        "/api/auth/password/request",
        "/api/auth/password/otp/send",
        "/api/auth/password/otp/validate",
        "/api/auth/password/reset",
        "/api/auth/confirm",
        "/api/ContactInfo/Internal/AuthInfo",
        "/api/ContactInfo/Internal/Profile",
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.WebSockets.IsWebSocketRequest)
        {
            await next(ctx); // downstream = SignalR
            return;
        }

        if (
            PublicEndpoints.Any(p =>
                ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            await next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var value) || value != ExpectedValue)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Access denied - outside gateway");
            return;
        }
        await next(ctx);
    }
}
