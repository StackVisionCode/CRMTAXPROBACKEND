using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedLibrary.Caching;

namespace SharedLibrary.Authorizations;

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger _log;
    private readonly IHttpContextAccessor _ctx;
    private readonly IHybridCache? _hybridCache;

    public PermissionHandler(
        IHttpClientFactory http,
        ILogger<PermissionHandler> log,
        IHttpContextAccessor ctx,
        IHybridCache? hybridCache = null
    )
    {
        _http = http;
        _log = log;
        _ctx = ctx;
        _hybridCache = hybridCache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        PermissionRequirement req
    )
    {
        /* 1 ¿ya venía el permiso como claim? */
        if (
            ctx
                .User.Claims.Where(c => c.Type == "perms")
                .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Contains(req.Code, StringComparer.OrdinalIgnoreCase)
        )
        {
            ctx.Succeed(req);
            return;
        }

        /* 2 consultar a AuthService (con cache 30 s) */
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return;

        var cacheKey = $"perms:{userId}";
        HashSet<string>? perms = null;

        // ✅ INTENTAR OBTENER DEL CACHÉ HÍBRIDO
        if (_hybridCache != null)
        {
            try
            {
                perms = await _hybridCache.GetAsync<HashSet<string>>(cacheKey);

                if (perms != null)
                {
                    _log.LogDebug(
                        "Permissions cache hit for user {UserId} using {CacheMode}",
                        userId,
                        _hybridCache.CurrentCacheMode
                    );
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error accessing hybrid cache for user permissions");
            }
        }

        // Si no hay permisos en caché, consultarlos del servicio
        if (perms == null)
        {
            var client = _http.CreateClient("AuthService");

            // Propagar el token de la petición entrante
            var bearer = _ctx.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(bearer))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(
                    bearer
                );

            try
            {
                // Ruta interna real del micro-servicio
                var list = await client.GetFromJsonAsync<IEnumerable<string>>(
                    $"/api/Permission/user/{userId}/codes"
                );

                perms = list?.ToHashSet(StringComparer.Ordinal) ?? new();

                // ✅ GUARDAR EN CACHÉ HÍBRIDO
                if (_hybridCache != null)
                {
                    try
                    {
                        await _hybridCache.SetAsync(cacheKey, perms, TimeSpan.FromSeconds(30));
                        _log.LogDebug(
                            "Permissions cached for user {UserId} using {CacheMode}",
                            userId,
                            _hybridCache.CurrentCacheMode
                        );
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "Failed to cache user permissions");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving permissions for user {UserId}", userId);
                return;
            }
        }

        if (perms != null && perms.Contains(req.Code))
            ctx.Succeed(req);
    }
}
