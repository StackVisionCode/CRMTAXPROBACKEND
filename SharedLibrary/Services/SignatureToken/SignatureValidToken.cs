using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;

namespace SharedLibrary.Services.SignatureToken;

/// <summary>
///  Servicio para emitir y validar tokens de firma (propósito = "sign").
/// </summary>
public sealed class SignatureValidToken(IOptions<JwtSettings> opt) : ISignatureValidToken
{
    private readonly JwtSettings _cfg = opt.Value;

    /* -----------------------------------------------------------
     * 1. Generación
     * --------------------------------------------------------- */
    public (string Token, DateTime Expires) Generate(
        Guid signerId,
        string requestId,
        string purpose
    )
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, signerId.ToString()),
                    new Claim("request_id", requestId),
                    new Claim("purpose", purpose),
                }
            ),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new(key, SecurityAlgorithms.HmacSha256),
        };

        var token = handler.CreateToken(descriptor);
        return (handler.WriteToken(token), token.ValidTo);
    }

    /* -----------------------------------------------------------
     * 2. Validación
     * --------------------------------------------------------- */
    public (bool IsValid, Guid SignerId, string RequestId) Validate(
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
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                },
                out _
            );

            // ► Propósito
            if (principal.FindFirst("purpose")?.Value != expectedPurpose)
                return (false, Guid.Empty, string.Empty);

            // ► Firmante y solicitud
            var signerIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            var requestIdClaim = principal.FindFirst("request_id");

            if (signerIdClaim is null || requestIdClaim is null)
                return (false, Guid.Empty, string.Empty);

            return (true, Guid.Parse(signerIdClaim.Value), requestIdClaim.Value); // ← string, no Guid
        }
        catch
        {
            return (false, Guid.Empty, string.Empty);
        }
    }
}
