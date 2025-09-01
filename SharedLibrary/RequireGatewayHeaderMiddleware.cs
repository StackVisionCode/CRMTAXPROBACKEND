using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace SharedLibrary
{
    /// <summary>
    /// Middleware que exige el header "X-From-Gateway: Api-Gateway" para TODAS las rutas
    /// que no sean explícitamente públicas. Distingue rutas de firma externas con token
    /// (no GUID) de las internas (GUID).
    /// </summary>
    public sealed class RequireGatewayHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        private const string GatewayHeaderName = "X-From-Gateway";
        private const string GatewayExpectedValue = "Api-Gateway";

        /* ------------------ RUTAS / ENDPOINTS PÚBLICOS EXACTOS (downstream) ------------------ */
        private static readonly string[] PublicExact =
        {
            // Auth / Password / Confirmaciones
            "/api/session/login",
            "/api/session/isvalid",
            "/api/password/request",
            "/api/password/otp/send",
            "/api/password/otp/validate",
            "/api/password/reset",
            "/api/account/confirm",
            "/api/auth/client/login",
            "/api/auth/login",
            "/api/taxuser/createcompany",
            "/api/taxcompany/register",
            "/api/auth/password/request",
            "/api/auth/password/otp/send",
            "/api/auth/password/otp/validate",
            "/api/auth/password/reset",
            "/api/auth/confirm",
            // Contact info internos que decidiste públicos
            "/api/contactinfo/internal/authinfo",
            "/api/contactinfo/internal/profile",
            // Firma (acciones públicas con token incorporado en el body)
            "/api/signaturerequests/consent",
            "/api/signaturerequests/submit",
            "/api/signaturerequests/reject",
            // RUTAS DE PREVIEW PÚBLICAS (downstream)
            "/api/signature/preview/info",
            "/api/signature/preview/status",
            "/api/signature/preview/access",
            "/api/signature/preview/invalidate",
            // Descarga documento final
            "/api/documentsigning/document",
            // Paises y States
            "/api/geography/countries",
            "/api/geography/states",
            "/api/geography/countries/{id}",
            "/api/geography/states/{id}",
            "/api/geography/countries/{countryId}/states",
            "/api/Company/CheckNameExists",
            "/api/taxuser/invite",
            "/api/taxuser/validate-invitation/{token}",
            "/api/taxcompany/checkcompanyname",
            "/api/public/company/{companyId}",
            "/api/public/taxuser/{taxUserId}",
            "/api/taxuser/invitations/by-token/{token}",
            "/api/subscriptions/custom-plans/create",
        };

        /* ------------------ RUTAS PÚBLICAS "UPSTREAM" (si las expones por gateway) ------------------ */
        private static readonly string[] PublicExactUpstream =
        {
            // Versión upstream (si el front las llama así)
            "/api/signature/consent",
            "/api/signature/submit",
            "/api/signature/reject",
            "/api/signature/validate", // si decides tener un endpoint directo validate (opcional)
            "/api/signature/layout", // idem
            "/api/signature/preview/info",
            "/api/signature/preview/status",
            "/api/signature/preview/access",
            "/api/signature/preview/invalidate",
        };

        /* ------------------ PREFIJOS PÚBLICOS FIJOS (layout con token al final) ------------------ */
        private static readonly string[] PublicPrefixes =
        {
            "/api/signaturerequests/layout/", // /api/SignatureRequests/layout/{token}
            "/api/signature/layout/", // upstream opcional
            // Preview disponible con signerId al final
            "/api/signature/preview/available/", // /api/signature/preview/available/{signerId}
        };

        /*
         * Prefijos base que llevan un segmento variable que puede ser:
         *   - un token alfanumérico (externo ⇒ público)
         *   - un GUID (interno ⇒ protegido)
         */
        private static readonly string[] BaseWithTokenOrGuid =
        {
            "/api/signaturerequests/", // controller downstream
            "/api/signature/validate/", // si decides upstream validate/{token}
            // Preview disponible puede recibir tanto signerId como GUID
            "/api/signature/preview/available/", // manejado arriba en PublicPrefixes
        };

        /* ------------------ WebSocket públicos (si tuvieras alguno) ------------------ */
        private static readonly string[] PublicWebSockets =
        {
            // Ejemplo: "/ws/public"
        };

        public RequireGatewayHeaderMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = (context.Request.Path.Value ?? string.Empty).TrimEnd('/'); // normaliza trailing slash
            var lower = path.ToLowerInvariant();

            bool isWebSocket = context.WebSockets.IsWebSocketRequest;

            if (IsPublic(lower, isWebSocket))
            {
                await _next(context);
                return;
            }

            // Validar header
            if (
                !context.Request.Headers.TryGetValue(GatewayHeaderName, out StringValues val)
                || val.Count == 0
                || !string.Equals(val[0], GatewayExpectedValue, StringComparison.Ordinal)
            )
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied - outside gateway");
                return;
            }

            await _next(context);
        }

        /* ------------------ LÓGICA DE CLASIFICACIÓN ------------------ */

        private static bool IsPublic(string lowerPath, bool isWs)
        {
            if (isWs)
            {
                // WebSockets: solo los que declares explícitamente públicos
                if (
                    PublicWebSockets.Any(p =>
                        lowerPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)
                    )
                )
                    return true;
                return false; // /ws normal => requiere gateway
            }

            // 1. Exactos downstream
            if (PublicExact.Any(e => lowerPath.Equals(e, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 2. Exactos upstream (si los usas)
            if (
                PublicExactUpstream.Any(e =>
                    lowerPath.Equals(e, StringComparison.OrdinalIgnoreCase)
                )
            )
                return true;

            // 3. Prefijos fijos - preview/available
            if (
                PublicPrefixes.Any(p => lowerPath.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            )
                return true;

            // 4. Caso especial: base + posible token o GUID
            if (
                BaseWithTokenOrGuid.Any(b =>
                    lowerPath.StartsWith(b, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                foreach (var b in BaseWithTokenOrGuid)
                {
                    if (!lowerPath.StartsWith(b, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var tail = lowerPath[b.Length..]; // lo que sigue del prefijo base

                    if (string.IsNullOrWhiteSpace(tail))
                        return false; // es exactamente el prefijo (sin token) => protegido (listado interno)

                    // Solo primer segmento
                    var firstSegment = tail.Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();
                    if (firstSegment is null)
                        return false;

                    // Para preview/available, todos los GUIDs son públicos (signerId)
                    if (b.Contains("/preview/available/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Para preview, tanto GUID (signerId) como tokens son públicos
                        return true;
                    }

                    // Para otras rutas: GUID => protegido
                    if (Guid.TryParse(firstSegment, out _))
                        return false;

                    // No GUID => lo tratamos como token externo ⇒ público
                    return true;
                }
            }

            return false;
        }
    }
}
