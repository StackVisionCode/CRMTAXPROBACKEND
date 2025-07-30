using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Common.Helpers;
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

        var displayName = DisplayNameHelper.From(
            req.User.Name,
            req.User.LastName,
            null,
            req.User.CompanyName,
            req.User.IsCompany,
            req.User.Email
        );

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

        // Claims de company
        claims.Add(new Claim("companyId", req.User.CompanyId.ToString()));
        claims.Add(new Claim("isCompany", req.User.IsCompany.ToString().ToLower()));

        if (!string.IsNullOrWhiteSpace(req.User.CompanyName))
            claims.Add(new Claim("companyName", req.User.CompanyName));

        if (!string.IsNullOrWhiteSpace(req.User.CompanyDomain))
            claims.Add(new Claim("companyDomain", req.User.CompanyDomain));

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
