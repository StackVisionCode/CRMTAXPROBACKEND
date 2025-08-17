using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;

namespace SharedLibrary.Services.InvitationService;

public sealed class InvitationTokenService : IInvitationTokenService
{
    private readonly JwtSettings _cfg;

    public InvitationTokenService(IOptions<JwtSettings> options)
    {
        _cfg = options.Value;
    }

    public (string Token, DateTime Expires) GenerateInvitation(
        Guid companyId,
        string email,
        ICollection<Guid>? roleIds = null
    )
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        var handler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new("company_id", companyId.ToString()),
            new("email", email),
            new("purpose", "taxuser_invitation"),
            new(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        // Agregar roles si se especifican
        if (roleIds?.Any() == true)
        {
            var rolesJson = JsonSerializer.Serialize(roleIds);
            claims.Add(new("roles", rolesJson));
        }

        var expires = DateTime.UtcNow.AddDays(2);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        var token = handler.CreateToken(descriptor);
        return (handler.WriteToken(token), expires);
    }

    public (
        bool IsValid,
        Guid CompanyId,
        string Email,
        ICollection<Guid>? RoleIds,
        string? ErrorMessage
    ) ValidateInvitation(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
            };

            var principal = handler.ValidateToken(
                token,
                validationParameters,
                out var validatedToken
            );

            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != "taxuser_invitation")
            {
                return (false, Guid.Empty, string.Empty, null, "Invalid invitation token purpose");
            }

            var companyIdClaim = principal.FindFirst("company_id")?.Value;
            var emailClaim = principal.FindFirst("email")?.Value;
            var rolesClaim = principal.FindFirst("roles")?.Value;

            if (!Guid.TryParse(companyIdClaim, out var companyId))
            {
                return (false, Guid.Empty, string.Empty, null, "Invalid company ID in token");
            }

            if (string.IsNullOrEmpty(emailClaim))
            {
                return (false, Guid.Empty, string.Empty, null, "Invalid email in token");
            }

            ICollection<Guid>? roleIds = null;
            if (!string.IsNullOrEmpty(rolesClaim))
            {
                try
                {
                    roleIds = JsonSerializer.Deserialize<ICollection<Guid>>(rolesClaim);
                }
                catch
                {
                    roleIds = null;
                }
            }

            return (true, companyId, emailClaim, roleIds, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, Guid.Empty, string.Empty, null, "Invitation token has expired");
        }
        catch (SecurityTokenException)
        {
            return (false, Guid.Empty, string.Empty, null, "Invalid invitation token");
        }
        catch (Exception ex)
        {
            return (
                false,
                Guid.Empty,
                string.Empty,
                null,
                $"Token validation failed: {ex.Message}"
            );
        }
    }
}
