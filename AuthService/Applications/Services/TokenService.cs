using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Infraestructure.Services;
using Microsoft.IdentityModel.Tokens;
using UserDTOS;

namespace AuthService.Applications.Services;

public class TokenService : ITokenService
{
  private readonly IConfiguration _configuration;
  public TokenService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public (string accessToken, DateTime expiry) GenerateAccessToken(int userId, string email, string name, TimeSpan lifeTime)
  {
    var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = Encoding.UTF8.GetBytes(secretKey);
        var handler = new JwtSecurityTokenHandler();
        var expiry = DateTime.UtcNow.Add(lifeTime);
        
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name ?? string.Empty)
            }),
            Expires = expiry,
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        });
        
        return (handler.WriteToken(token), expiry);
  }

  public string GetEmailFromToken(string token)
  {
    var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "nameid");
        
        return emailClaim?.Value ?? throw new InvalidOperationException("Invalid token format: Email claim not found");
  }

  public int GetUserIdFromToken(string token)
  {
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);
    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
        
    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
      return userId;
            
    throw new InvalidOperationException("Invalid token format: UserId claim not found or invalid");
  }

  public bool ValidateToken(string token)
  {
    if (string.IsNullOrEmpty(token))
            return false;

        var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = Encoding.UTF8.GetBytes(secretKey);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
  }
}