using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;

namespace SharedLibrary.Services.ConfirmAccountService;

internal sealed class ConfirmTokenService(IOptions<JwtSettings> opt) : IConfirmTokenService
{
  private readonly JwtSettings _cfg = opt.Value;

  public (string Token, DateTime Expires) Generate(Guid uid, string email)
  {
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg.SecretKey));
    var handler = new JwtSecurityTokenHandler();

    var descriptor = new SecurityTokenDescriptor
    {
      Subject = new ClaimsIdentity(
            new[]
            {
                    new Claim(JwtRegisteredClaimNames.Sub, uid.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim("purpose", "confirm_account")
            }),
      Expires = DateTime.UtcNow.AddHours(1),
      SigningCredentials = new(key, SecurityAlgorithms.HmacSha256)
    };

    var token = handler.CreateToken(descriptor);
    return (handler.WriteToken(token), token.ValidTo);
  }
}