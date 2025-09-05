using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SharedLibrary.Extensions;

public static class JwtOptionsFactory
{
    public static TokenValidationParameters Build(IConfigurationSection cfg)
    {
        var audiences = (cfg["Audience"] ?? "").Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        return new TokenValidationParameters
        {
            ValidateIssuer = bool.Parse(cfg["ValidateIssuer"] ?? "false"),
            ValidateAudience = bool.Parse(cfg["ValidateAudience"] ?? "false"),
            ValidateLifetime = bool.Parse(cfg["ValidateLifetime"] ?? "true"),
            ValidateIssuerSigningKey = bool.Parse(cfg["ValidateIssuerSigningKey"] ?? "true"),
            ValidIssuer = cfg["Issuer"],
            ValidAudience = audiences.Length == 1 ? audiences[0] : null,
            ValidAudiences = audiences.Length > 1 ? audiences : null,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["SecretKey"]!)),
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    }
}
