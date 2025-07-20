using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;

namespace SharedLibrary.Services.SignatureToken;

/// <summary>
///  Servicio para emitir y validar tokens de firma (propósito = "sign").
/// </summary>
public sealed class SignatureValidToken : ISignatureValidToken
{
    private readonly JwtSettings _cfg;
    private readonly ILogger<SignatureValidToken> _logger;

    public SignatureValidToken(IOptions<JwtSettings> opt, ILogger<SignatureValidToken> logger)
    {
        _cfg = opt.Value;
        _logger = logger;
    }

    /* -----------------------------------------------------------
     * 1. Generación
     * --------------------------------------------------------- */
    public (string Token, DateTime Expires) Generate(Guid signerId, Guid requestId, string purpose)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim("sub", signerId.ToString()),
                    new Claim("request_id", requestId.ToString()),
                    new Claim("purpose", purpose),
                }
            ),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _cfg.Issuer,
            Audience = _cfg.Audience,
            SigningCredentials = new(key, SecurityAlgorithms.HmacSha256),
        };

        var token = handler.CreateToken(descriptor);
        var tokenString = handler.WriteToken(token);

        _logger.LogInformation(
            "✅ Token generado para SignerId: {SignerId}, RequestId: {RequestId}, Purpose: {Purpose}",
            signerId,
            requestId,
            purpose
        );

        return (tokenString, token.ValidTo);
    }

    /* -----------------------------------------------------------
     * 2. Validación (CORREGIDA)
     * --------------------------------------------------------- */
    public (bool IsValid, Guid SignerId, Guid RequestId) Validate(
        string token,
        string expectedPurpose
    )
    {
        _logger.LogInformation(
            "🔍 [SignatureValidToken] Iniciando validación con purpose: {Purpose}",
            expectedPurpose
        );

        // ✅ VALIDACIONES INICIALES
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("❌ [SignatureValidToken] Token es null o vacío");
            return (false, Guid.Empty, Guid.Empty);
        }

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
        {
            _logger.LogWarning("❌ [SignatureValidToken] Token no tiene formato JWT válido");
            return (false, Guid.Empty, Guid.Empty);
        }

        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey)),
                ValidateIssuer = _cfg.ValidateIssuer,
                ValidIssuer = _cfg.Issuer,
                ValidateAudience = _cfg.ValidateAudience,
                ValidAudience = _cfg.Audience,
                ValidateLifetime = _cfg.ValidateLifetime,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = handler.ValidateToken(token, tokenValidationParameters, out _);

            _logger.LogInformation("✅ [SignatureValidToken] Token JWT validado correctamente");

            // ✅ EXTRAER CLAIMS CON MÉTODOS ALTERNATIVOS
            var subClaim =
                principal.FindFirst("sub")
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            var reqIdClaim = principal.FindFirst("request_id");
            var purposeClaim = principal.FindFirst("purpose");

            _logger.LogInformation("🔍 [SignatureValidToken] Claims encontrados:");
            _logger.LogInformation("  - sub: {Sub}", subClaim?.Value ?? "NULL");
            _logger.LogInformation("  - request_id: {RequestId}", reqIdClaim?.Value ?? "NULL");
            _logger.LogInformation("  - purpose: {Purpose}", purposeClaim?.Value ?? "NULL");

            // ✅ DEBUGGING: Si sub es null, mostrar todos los claims
            if (subClaim == null)
            {
                _logger.LogWarning(
                    "❌ [SignatureValidToken] Claim 'sub' no encontrado. Todos los claims disponibles:"
                );
                foreach (var claim in principal.Claims)
                {
                    _logger.LogWarning(
                        "  - Type: '{Type}', Value: '{Value}'",
                        claim.Type,
                        claim.Value
                    );
                }
                return (false, Guid.Empty, Guid.Empty);
            }

            // ✅ VALIDAR PURPOSE
            if (purposeClaim?.Value != expectedPurpose)
            {
                _logger.LogWarning(
                    "❌ [SignatureValidToken] Purpose mismatch. Expected: {Expected}, Found: {Found}",
                    expectedPurpose,
                    purposeClaim?.Value ?? "null"
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            // ✅ VALIDAR QUE TODOS LOS CLAIMS ESENCIALES ESTÉN PRESENTES
            if (reqIdClaim is null)
            {
                _logger.LogWarning(
                    "❌ [SignatureValidToken] Claim 'request_id' faltante en el token"
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            // ✅ PARSEAR Y VALIDAR GUIDs
            if (!Guid.TryParse(subClaim.Value, out var signerId))
            {
                _logger.LogWarning(
                    "❌ [SignatureValidToken] Sub no es un GUID válido: {Sub}",
                    subClaim.Value
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            if (!Guid.TryParse(reqIdClaim.Value, out var requestId))
            {
                _logger.LogWarning(
                    "❌ [SignatureValidToken] RequestId no es un GUID válido: {RequestId}",
                    reqIdClaim.Value
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            _logger.LogInformation(
                "✅ [SignatureValidToken] Validación exitosa - SignerId: {SignerId}, RequestId: {RequestId}",
                signerId,
                requestId
            );

            return (true, signerId, requestId);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("❌ [SignatureValidToken] Token expirado: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogError(
                "❌ [SignatureValidToken] Firma de token inválida: {Message}",
                ex.Message
            );
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogError(
                "❌ [SignatureValidToken] Issuer de token inválido: {Message}",
                ex.Message
            );
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogError(
                "❌ [SignatureValidToken] Audience de token inválido: {Message}",
                ex.Message
            );
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("❌ [SignatureValidToken] Error parseando GUID: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SignatureValidToken] Error inesperado al validar token");
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
