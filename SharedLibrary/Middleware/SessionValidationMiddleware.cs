using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Middleware;

public sealed class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<SessionValidationMiddleware> _logger;
    private readonly IMemoryCache _cache;

    public SessionValidationMiddleware(
        RequestDelegate next,
        IHttpClientFactory factory,
        ILogger<SessionValidationMiddleware> logger,
        IMemoryCache cache
    )
    {
        _next = next;
        _factory = factory;
        _logger = logger;
        _cache = cache;
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

        // Intentar obtener del caché
        if (!_cache.TryGetValue(cacheKey, out string? cachedResult))
        {
            try
            {
                _logger.LogDebug(
                    "Cache miss for {CacheKey}, validating with auth service",
                    cacheKey
                );
                var client = _factory.CreateClient("Auth");
                var resp = await client.GetAsync($"/api/Session/IsValid?sid={sid}");
                cachedResult = resp.IsSuccessStatusCode ? bool.TrueString : bool.FalseString;

                // Guardar en caché con expiración
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2),
                    Size = 1, // Para control de tamaño de caché si se implementa límite
                };

                _cache.Set(cacheKey, cachedResult, cacheOptions);
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
        else
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
        }

        if (cachedResult != bool.TrueString)
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
