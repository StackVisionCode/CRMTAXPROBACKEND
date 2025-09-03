using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common;
using LandingService.Applications.Services;
using LandingService.Domain;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.Context;
using LandingService.Infrastructure.Services;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.DTOs;

namespace LandingService.Applications.Handlers;

public class LoginHandler : IRequestHandler<LoginCommands, ApiResponse<string>>
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IConfiguration _cfg;

    public LoginHandler(IMediator mediator, ApplicationDbContext context, ILogger<LoginHandler> logger, IConfiguration cfg)
    {
        _mediator = mediator;
        _context = context;
        _logger = logger;
        _cfg = cfg;
    }

    public async Task<ApiResponse<string>> Handle(LoginCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var validated = await _context.Users.FindAsync(request.Login.Email);
            if (validated == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                };
            }

            if (validated.Password != request.Login.Password)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Invalid password",
                    Data = null
                };
            }

            if (validated.IsActive == false)
            {
                
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "User is inactive",
                    Data = null
                };  
            }

            var Token= Generate(validated, request.Login.Remember);
            if (string.IsNullOrEmpty(Token))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Token generation failed",
                    Data = null
                };  
            }

            CreateTaxUserSessionAsync(
                validated.Id,
                Guid.NewGuid(),                
                request,
                cancellationToken
            ).Wait(cancellationToken);
         

            // Lógica de manejo del comando de inicio de sesión
            return new ApiResponse<string>
            {
                Success = true,
                Message = "Login successful",
                Data = Token// Aquí iría el token generado
            };

        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Error during login process");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred during login",
                Data = null
            };
        }
    }

  private async  Task CreateTaxUserSessionAsync(
        Guid userId,
        Guid sessionId,        
        LoginCommands request,
        CancellationToken ct
    )
    {
        // Obtener geolocalización
        GeolocationInfo? geoInfo = null;
        string? displayLocation = null;

        try
        {
            
          
            _logger.LogDebug(
              
                  displayLocation,
                geoInfo?.Country,
                geoInfo?.City
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, ex.Message.ToString());
            displayLocation = "Unknown Location";
        }

        var session = new Session
        {
            Id = sessionId,
            UserId =userId,
            TokenRequest ="",
            ExpireTokenRequest = DateTime.UtcNow,
            TokenRefresh = "",
            IpAddress = "",
            Device = "",
            IsRevoke = false,
            CreatedAt = DateTime.UtcNow,
            Country = geoInfo?.Country,
            City = geoInfo?.City,
            Region = geoInfo?.Region,
            Latitude = geoInfo?.Latitude?.ToString("F6"),
            Longitude = geoInfo?.Longitude?.ToString("F6"),
            Location = displayLocation,
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(ct);
    }
  private string Generate(User req, bool rememberMe)
    {
      
        var resultKey = _cfg.GetSection("JwtSettings").Get<JwtSettings>();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(resultKey!.SecretKey));
        var handler = new JwtSecurityTokenHandler();

     
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, req.Id.ToString()),
            new(ClaimTypes.NameIdentifier, req.Id.ToString()),
            new(ClaimTypes.Email, req.Email),
            new(ClaimTypes.Name, req.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        
        };


        if (!string.IsNullOrWhiteSpace(req.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, req.LastName));

      

            var expire =  rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1);


        var token = handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expire,
                Issuer = resultKey.Issuer,
                Audience = resultKey.Audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            }
        );

        return new(handler.WriteToken(token));
    }
}