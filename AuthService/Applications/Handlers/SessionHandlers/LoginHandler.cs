using AuthService.Applications.DTOs.RabbitDTOs;
using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AuthService.Infraestructure.Services;
using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SessionHandlers;

public class LoginHandler : IRequestHandler<LoginCommands, ApiResponse<LoginResponseDTO>>
{
  private readonly ApplicationDbContext _context;
  private readonly IPasswordHash _passwordHasher;
  private readonly ITokenService _tokenService;
  private readonly ILogger<LoginHandler> _logger;
  private readonly IRabbitEventBus _bus;
  public LoginHandler(ApplicationDbContext context, IPasswordHash passwordHasher, ITokenService tokenService, ILogger<LoginHandler> logger, IRabbitEventBus bus)
  {
    _context = context;
    _passwordHasher = passwordHasher;
    _tokenService = tokenService;
    _logger = logger;
    _bus = bus;
  }

  public async Task<ApiResponse<LoginResponseDTO>> Handle(LoginCommands request, CancellationToken cancellationToken)
  {
    try
        {
            // 1. Validamos credenciales
            var user = await _context.TaxUsers
                                    .Include(s => s.Session)
                                    .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user is null || !_passwordHasher.Verify(request.Password, user.Password))
            {
                _logger.LogWarning("Login failed for user {Email}: Invalid credentials", request.Email);
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed for user {Email}: User is inactive", request.Email);
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            // 2. Generamos token
            TimeSpan accessTokenLifetime = request.RememberMe ? TimeSpan.FromDays(1) : TimeSpan.FromHours(1);
            var (accessToken, accessTokenExpiry) = _tokenService.GenerateAccessToken(
                user.Id, user.Email, user.FullName ?? "", accessTokenLifetime);

            var (refreshToken, _) = _tokenService.GenerateAccessToken(
                user.Id, user.Email, user.FullName ?? "", TimeSpan.FromDays(2));

            // 3. Creamos sesión
            var session = new Session
            {
                TaxUser = user,
                TaxUserId = user.Id,
                TokenRequest = accessToken,
                ExpireTokenRequest = accessTokenExpiry,
                TokenRefresh = refreshToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Preparamos una respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = accessToken,
                ExpireTokenRequest = accessTokenExpiry
            };

            _logger.LogInformation("User {Id} logged-in successfully. Session {SessionId} created", user.Id, session.Id);
            var loginEvent = new LoginEvent(user.Id, user.Email, request.IpAddress, request.Device, DateTime.UtcNow);
            _bus.Publish("auth.events", "auth.login", loginEvent);
            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for {Email}", request.Email);
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
  }
}