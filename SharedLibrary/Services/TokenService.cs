using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace SharedLibrary.Services;

internal sealed class TokenService : ITokenService
{
    private readonly JwtSettings _cfg;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IOptions<JwtSettings> options, ILogger<TokenService> logger)
    {
        _cfg = options.Value;
        _logger = logger;
    }

    public TokenResult Generate(TokenGenerationRequest req)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        // Construir nombre completo correctamente
        var completeName = $"{req.User.Name} {req.User.LastName}".Trim();

        // Si viene CompanyName ⇒ ese será el ClaimTypes.Name
        var displayName = string.IsNullOrWhiteSpace(req.User.CompanyName)
            ? completeName
            : req.User.CompanyName!;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, req.User.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, req.User.UserId.ToString()),
            new(ClaimTypes.Email, req.User.Email),
            new(ClaimTypes.Name, displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("sid", req.Session.Id.ToString()),
        };

        // estándar + custom
        if (!string.IsNullOrWhiteSpace(req.User.Name))
            claims.Add(new Claim(ClaimTypes.GivenName, req.User.Name));

        if (!string.IsNullOrWhiteSpace(req.User.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, req.User.LastName));

        if (!string.IsNullOrWhiteSpace(req.User.Address))
            claims.Add(new Claim("address", req.User.Address));

        if (!string.IsNullOrWhiteSpace(req.User.PhotoUrl))
            claims.Add(new Claim("picture", req.User.PhotoUrl));

        if (req.User.CompanyId != Guid.Empty)
            claims.Add(new Claim("companyId", req.User.CompanyId.ToString()));

        if (!string.IsNullOrWhiteSpace(req.User.CompanyName))
            claims.Add(new Claim("companyName", req.User.CompanyName));

        if (!string.IsNullOrWhiteSpace(req.User.FullName))
            claims.Add(new Claim("fullName", req.User.FullName));

        if (!string.IsNullOrWhiteSpace(req.User.CompanyBrand))
            claims.Add(new Claim("companyBrand", req.User.CompanyBrand));

        foreach (var role in req.User.Roles.Distinct())
            claims.Add(new Claim("roles", role));

        foreach (var perm in req.User.Permissions.Distinct())
            claims.Add(new Claim("perms", perm));
        foreach (var portal in req.User.Portals.Distinct())
            claims.Add(new("portal", portal));

        var token = handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(req.LifeTime),
                Issuer = _cfg.Issuer,
                Audience = _cfg.Audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            }
        );

        return new(handler.WriteToken(token), token.ValidTo);
    }
}
