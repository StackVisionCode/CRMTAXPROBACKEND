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
                    new Claim("sub", signerId.ToString()), // Solo usar "sub" simple
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

        // Log token generation details
        _logger.LogInformation(
            "Token generado - SignerId: {SignerId}, RequestId: {RequestId}, Purpose: {Purpose}, Expires: {Expires}",
            signerId,
            requestId,
            purpose,
            token.ValidTo
        );
        _logger.LogDebug("Token string: {Token}", tokenString);

        return (tokenString, token.ValidTo);
    }

    /* -----------------------------------------------------------
     * 2. Validación
     * --------------------------------------------------------- */
    public (bool IsValid, Guid SignerId, Guid RequestId) Validate(
        string token,
        string expectedPurpose
    )
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            // First, try to read the token without validation to see its contents
            try
            {
                var jsonToken = handler.ReadJwtToken(token);
                _logger.LogInformation("Token claims sin validación:");
                foreach (var claim in jsonToken.Claims)
                {
                    _logger.LogInformation("- {Type}: {Value}", claim.Type, claim.Value);
                }
                _logger.LogInformation("Token Issuer: {Issuer}", jsonToken.Issuer);
                _logger.LogInformation(
                    "Token Audience: {Audience}",
                    string.Join(", ", jsonToken.Audiences)
                );
                _logger.LogInformation("Token ValidFrom: {ValidFrom}", jsonToken.ValidFrom);
                _logger.LogInformation("Token ValidTo: {ValidTo}", jsonToken.ValidTo);
            }
            catch (Exception readEx)
            {
                _logger.LogError(
                    readEx,
                    "No se puede leer el token sin validación. Token posiblemente corrupto."
                );

                // Intentar decodificar manualmente el payload
                try
                {
                    var parts = token.Split('.');
                    if (parts.Length == 3)
                    {
                        var payload = parts[1];
                        // Ajustar padding si es necesario
                        switch (payload.Length % 4)
                        {
                            case 2:
                                payload += "==";
                                break;
                            case 3:
                                payload += "=";
                                break;
                        }

                        var jsonBytes = Convert.FromBase64String(
                            payload.Replace('-', '+').Replace('_', '/')
                        );
                        var json = Encoding.UTF8.GetString(jsonBytes);
                        _logger.LogInformation("Payload JSON crudo: {Payload}", json);
                    }
                }
                catch (Exception decodeEx)
                {
                    _logger.LogError(decodeEx, "Error al decodificar payload manualmente");
                }

                return (false, Guid.Empty, Guid.Empty);
            }

            var principal = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_cfg.SecretKey)
                    ),
                    ValidateIssuer = _cfg.ValidateIssuer,
                    ValidIssuer = _cfg.Issuer,
                    ValidateAudience = _cfg.ValidateAudience,
                    ValidAudience = _cfg.Audience,
                    ValidateLifetime = _cfg.ValidateLifetime,
                    ClockSkew = TimeSpan.Zero,
                },
                out _
            );

            _logger.LogInformation("Token validado exitosamente");

            // 1. propósito
            var purposeClaim = principal.FindFirst("purpose");
            if (purposeClaim?.Value != expectedPurpose)
            {
                _logger.LogWarning(
                    "Purpose mismatch. Expected: {Expected}, Found: {Found}",
                    expectedPurpose,
                    purposeClaim?.Value ?? "null"
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            // 2. claims esenciales - Buscar solo por "sub"
            var subClaim = principal.FindFirst("sub");
            var reqIdClaim = principal.FindFirst("request_id");

            _logger.LogInformation("Claims encontrados:");
            foreach (var claim in principal.Claims)
            {
                _logger.LogInformation("- {Type}: {Value}", claim.Type, claim.Value);
            }

            if (subClaim is null || reqIdClaim is null)
            {
                _logger.LogWarning(
                    "Claims esenciales faltantes. Sub: {Sub}, RequestId: {RequestId}",
                    subClaim?.Value ?? "null",
                    reqIdClaim?.Value ?? "null"
                );
                return (false, Guid.Empty, Guid.Empty);
            }

            var signerId = Guid.Parse(subClaim.Value);
            var requestId = Guid.Parse(reqIdClaim.Value);

            _logger.LogInformation(
                "Token válido - SignerId: {SignerId}, RequestId: {RequestId}",
                signerId,
                requestId
            );

            return (true, signerId, requestId);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("Token expirado: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogError("Firma de token inválida: {Message}", ex.Message);
            _logger.LogError("Esto generalmente indica que la SecretKey no coincide");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogError("Issuer inválido: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogError("Audience inválido: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al validar token: {Message}", ex.Message);
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
