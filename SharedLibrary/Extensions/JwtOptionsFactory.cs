using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SharedLibrary.Extensions;

public static class JwtOptionsFactory
{
    public static TokenValidationParameters Build(IConfigurationSection cfg) =>
        new()
        {
            ValidateIssuer = bool.Parse(cfg["ValidateIssuer"] ?? "false"),
            ValidateAudience = bool.Parse(cfg["ValidateAudience"] ?? "false"),
            ValidateLifetime = bool.Parse(cfg["ValidateLifetime"] ?? "true"),
            ValidateIssuerSigningKey = bool.Parse(cfg["ValidateIssuerSigningKey"] ?? "true"),
            ValidIssuer = cfg["Issuer"],
            ValidAudience = cfg["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["SecretKey"]!)),
            ClockSkew = TimeSpan.Zero,
        };
}
