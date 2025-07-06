using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Authorizations;

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _http;
    private readonly ILogger _log;
    private readonly IHttpContextAccessor _ctx;

    public PermissionHandler(
        IMemoryCache cache,
        IHttpClientFactory http,
        ILogger<PermissionHandler> log,
        IHttpContextAccessor ctx
    ) // 👈
    {
        _cache = cache;
        _http = http;
        _log = log;
        _ctx = ctx;
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
        if (!_cache.TryGetValue(cacheKey, out HashSet<string>? perms))
        {
            var client = _http.CreateClient("AuthService");

            /*  propagar el token de la petición entrante */
            var bearer = _ctx.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(bearer))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(
                    bearer
                );

            /*  ruta interna real del micro-servicio               */
            var list = await client.GetFromJsonAsync<IEnumerable<string>>(
                $"/api/Permission/user/{userId}/codes"
            );

            perms = list?.ToHashSet(StringComparer.Ordinal) ?? new();
            _cache.Set(
                cacheKey,
                perms,
                new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                    .SetSize(1)
            );
        }

        if (perms != null && perms.Contains(req.Code))
            ctx.Succeed(req);
    }
}
