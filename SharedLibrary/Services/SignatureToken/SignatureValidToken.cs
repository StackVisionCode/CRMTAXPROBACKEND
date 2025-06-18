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
    public (string Token, DateTime Expires) Generate(Guid signerId, Guid requestId, string purpose)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, signerId.ToString()),
                    new Claim("request_id", requestId.ToString()),
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
    public (bool IsValid, Guid SignerId, Guid RequestId) Validate(string token, string expected)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var p = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_cfg.SecretKey)
                    ),
                },
                out _
            );

            if (p.FindFirst("purpose")?.Value != expected)
                return (false, Guid.Empty, Guid.Empty);

            return (
                true,
                Guid.Parse(p.FindFirst(JwtRegisteredClaimNames.Sub)!.Value),
                Guid.Parse(p.FindFirst("request_id")!.Value)
            );
        }
        catch
        {
            return (false, Guid.Empty, Guid.Empty);
        }
    }
}
