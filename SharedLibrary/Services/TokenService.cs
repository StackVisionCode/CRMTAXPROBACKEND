using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace SharedLibrary.Services;

internal sealed class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _cfg = options.Value;

    public TokenResult Generate(TokenGenerationRequest r)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, r.UserId.ToString()),
                new Claim(ClaimTypes.Email, r.Email),
                new Claim(ClaimTypes.Name, r.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("sid", r.SessionId)
            }),
            Expires = DateTime.UtcNow.Add(r.LifeTime),
            Issuer = _cfg.Issuer,
            Audience = _cfg.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });

        return new TokenResult(handler.WriteToken(token), token.ValidTo);
    }
}