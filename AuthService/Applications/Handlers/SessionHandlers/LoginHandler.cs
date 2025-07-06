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
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

namespace Handlers.SessionHandlers;

public class LoginHandler : IRequestHandler<LoginCommands, ApiResponse<LoginResponseDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHash _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IEventBus _eventBus;

    public LoginHandler(
        ApplicationDbContext context,
        IPasswordHash passwordHasher,
        ITokenService tokenService,
        ILogger<LoginHandler> logger,
        IEventBus eventBus
    )
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<LoginResponseDTO>> Handle(
        LoginCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Validamos credenciales
            var user = await _context
                .TaxUsers.Include(u => u.TaxUserProfile)
                .Include(u => u.Company)
                .Include(s => s.Sessions)
                .FirstOrDefaultAsync(x => x.Email == request.Petition.Email, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // 1) Consultar roles y permisos
            var roleNames = await _context
                .UserRoles.Where(ur => ur.TaxUserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync(cancellationToken);

            var portals = await _context
                .Roles.Where(r => roleNames.Contains(r.Name))
                .Select(r => r.PortalAccess.ToString()) // enum → string
                .Distinct()
                .ToListAsync(cancellationToken);

            var permCodes = await (
                from ur in _context.UserRoles
                where ur.TaxUserId == user.Id
                join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                select p.Code
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            bool allowed = await _context
                .Roles.Where(r => roleNames.Contains(r.Name)) // ← COMPARO POR NOMBRE
                .AnyAsync(
                    r =>
                        r.PortalAccess == PortalAccess.Staff // STAFF   ✔
                        || r.PortalAccess == PortalAccess.Both, // BOTH    ✔
                    cancellationToken
                );

            if (!allowed)
            {
                _logger.LogWarning(
                    "Role {Roles} not authorized for Staff login",
                    string.Join(",", roleNames)
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "You do not have permission to log in here."
                )
                {
                    StatusCode = 403,
                }; // ◄─ sin crear sesión
            }

            if (!_passwordHasher.Verify(request.Petition.Password, user.Password))
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            var sessionId = Guid.NewGuid();

            // 2. Objetos compuestos para el token
            var profile = user.TaxUserProfile;
            var companyId = user.Company?.Id;
            var companyName = user.Company?.CompanyName;
            var fullName = user.Company?.FullName;
            var companyBrand = user.Company?.Brand;

            var userInfo = new UserInfo(
                user.Id,
                user.Email,
                profile?.Name ?? string.Empty,
                profile?.LastName ?? string.Empty,
                profile?.Address ?? string.Empty,
                profile?.PhotoUrl ?? string.Empty,
                companyId ?? Guid.Empty,
                companyName ?? string.Empty,
                fullName ?? string.Empty,
                companyBrand ?? string.Empty,
                roleNames,
                permCodes,
                portals
            );

            var sessionInfo = new SessionInfo(sessionId);

            var access = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(1))
            );

            var refresh = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(2))
            );

            // 3. Creamos sesión
            var session = new Session
            {
                Id = sessionId, // Use the same sessionId as above
                TaxUser = user,
                TaxUserId = user.Id,
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            // Determinar el nombre a mostrar
            string displayName = DetermineDisplayName(
                profile?.Name,
                profile?.LastName,
                companyName
            );

            // 3.5 Publicamos un evento de sesión creada
            var loginEvent = new UserLoginEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                user.Id,
                user.Email,
                profile?.Name ?? string.Empty,
                profile?.LastName ?? string.Empty,
                DateTime.UtcNow,
                request.IpAddress,
                request.Device,
                user.CompanyId ?? Guid.Empty,
                companyName ?? string.Empty,
                fullName ?? string.Empty,
                displayName, // <-- Nombre Completo de Usuario Individual calculado
                DateTime.Now.Year
            );

            // 3.6 Publicamos un evento de para indicar que estamo online en CommLinkService
            _eventBus.Publish(
                new UserPresenceChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    user.Id,
                    "TaxUser",
                    true
                )
            );

            _eventBus.Publish(loginEvent);

            // 4. Preparamos una respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _logger.LogInformation(
                "User {Id} logged-in successfully. Session {SessionId} created",
                user.Id,
                session.Id
            );
            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for {Email}", request.Petition.Email);
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
    }

    private static string DetermineDisplayName(string? name, string? lastName, string? companyName)
    {
        // Si es un usuario individual (tiene nombre o apellido)
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(lastName))
        {
            return $"{name} {lastName}".Trim();
        }

        // Si es una oficina/empresa (solo tiene companyName)
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            return companyName;
        }

        // Fallback
        return "Usuario";
    }
}
