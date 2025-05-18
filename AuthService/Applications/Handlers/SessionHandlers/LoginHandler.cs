using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using AuthService.Infraestructure.Services;
using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace Handlers.SessionHandlers;

public class LoginHandler : IRequestHandler<LoginCommands, ApiResponse<LoginResponseDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHash _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginHandler> _logger;
    public LoginHandler(ApplicationDbContext context, IPasswordHash passwordHasher, ITokenService tokenService, ILogger<LoginHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
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

            var sessionId = Guid.NewGuid().ToString();

            // Define the access token lifetime (e.g., 1 hour)
            var accessTokenLifetime = TimeSpan.FromHours(1);

            var access = _tokenService.Generate(new TokenGenerationRequest(
                    user.Id, user.Email, user.FullName ?? string.Empty, sessionId,
                    accessTokenLifetime));

            var refresh = _tokenService.Generate(new TokenGenerationRequest(
                    user.Id, user.Email, user.FullName ?? string.Empty, sessionId,
                    TimeSpan.FromDays(2)));

            // 3. Creamos sesión
            var session = new Session
            {
                TaxUser = user,
                TaxUserId = user.Id,
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
                SessionUid         = sessionId,
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
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _logger.LogInformation("User {Id} logged-in successfully. Session {SessionId} created", user.Id, session.Id);
            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for {Email}", request.Email);
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
    }
}