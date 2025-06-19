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
            "Token generado para SignerId: {SignerId}, RequestId: {RequestId}, Purpose: {Purpose}",
            signerId,
            requestId,
            purpose
        );

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

            // Validar propósito
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

            // Extraer claims esenciales
            var subClaim = principal.FindFirst("sub");
            var reqIdClaim = principal.FindFirst("request_id");

            if (subClaim is null || reqIdClaim is null)
            {
                _logger.LogWarning("Claims esenciales faltantes en el token");
                return (false, Guid.Empty, Guid.Empty);
            }

            var signerId = Guid.Parse(subClaim.Value);
            var requestId = Guid.Parse(reqIdClaim.Value);

            return (true, signerId, requestId);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expirado");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogError("Firma de token inválida");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            _logger.LogError("Issuer de token inválido");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogError("Audience de token inválido");
            return (false, Guid.Empty, Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar token");
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
