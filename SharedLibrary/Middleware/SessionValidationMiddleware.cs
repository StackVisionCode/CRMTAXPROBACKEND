using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedLibrary.Caching;

namespace SharedLibrary.Middleware;

public sealed class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<SessionValidationMiddleware> _logger;
    private readonly IHybridCache? _hybridCache;

    public SessionValidationMiddleware(
        RequestDelegate next,
        IHttpClientFactory factory,
        ILogger<SessionValidationMiddleware> logger,
        IHybridCache? hybridCache = null
    )
    {
        _next = next;
        _factory = factory;
        _logger = logger;
        _hybridCache = hybridCache;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var bearer = ctx.Request.Headers.Authorization.FirstOrDefault();
        if (bearer is null || !bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var token = bearer["Bearer ".Length..];
        var sid = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims.FirstOrDefault(c => c.Type == "sid")
            ?.Value;

        if (sid is null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Token sin claim 'sid'");
            return;
        }

        string cacheKey = $"sid:{sid}";
        bool? cachedResult = null;

        // USAR CACHÉ HÍBRIDO SI ESTÁ DISPONIBLE
        if (_hybridCache != null)
        {
            try
            {
                cachedResult = await _hybridCache.GetAsync<bool?>(cacheKey);

                if (cachedResult.HasValue)
                {
                    _logger.LogDebug(
                        "Session validation cache hit for SID {SessionId} using {CacheMode}",
                        sid,
                        _hybridCache.CurrentCacheMode
                    );
                }
                else
                {
                    _logger.LogDebug("Session validation cache miss for SID {SessionId}", sid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing hybrid cache for session validation");
                cachedResult = null;
            }
        }

        // Si no hay resultado en caché, validar con el servicio de auth
        if (!cachedResult.HasValue)
        {
            try
            {
                _logger.LogDebug("Validating session {SessionId} with auth service", sid);

                var client = _factory.CreateClient("Auth");
                var resp = await client.GetAsync($"/api/Session/IsValid?sid={sid}");
                cachedResult = resp.IsSuccessStatusCode;

                // Guardar en caché híbrido si está disponible
                if (_hybridCache != null)
                {
                    try
                    {
                        var cacheExpiry = cachedResult.Value
                            ? TimeSpan.FromSeconds(30)
                            : TimeSpan.FromSeconds(5);
                        await _hybridCache.SetAsync(cacheKey, cachedResult.Value, cacheExpiry);

                        _logger.LogDebug(
                            "Session validation result cached for SID {SessionId} using {CacheMode}, result: {Result}",
                            sid,
                            _hybridCache.CurrentCacheMode,
                            cachedResult.Value
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache session validation result");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error validating session with auth service for SID {SessionId}",
                    sid
                );
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync("Error validando sesión");
                return;
            }
        }

        if (!cachedResult.GetValueOrDefault())
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Sesión revocada o expirada");
            return;
        }
        ctx.Items["SessionId"] = sid;

        // Añadir también SessionUid como claim en el User.Identity
        // Esto permitirá acceder al valor mediante User.FindFirst("sid")
        var identity = ctx.User.Identity as System.Security.Claims.ClaimsIdentity;
        if (identity != null)
        {
            // Verificar si el claim ya existe y actualizarlo
            var existingClaim = identity.FindFirst("sid");
            if (existingClaim != null)
            {
                identity.RemoveClaim(existingClaim);
            }

            // Añadir el claim
            identity.AddClaim(new System.Security.Claims.Claim("sid", sid));
        }

        await _next(ctx);
    }
}

public static class SessionValidationAppBuilderExtensions
{
    public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<SessionValidationMiddleware>();
}
