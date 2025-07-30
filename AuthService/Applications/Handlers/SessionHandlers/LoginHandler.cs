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
            // 1. Buscar usuario con Company usando JOIN (sin Include)
            var userQuery =
                from u in _context.TaxUsers
                join c in _context.Companies on u.CompanyId equals c.Id into companies
                from c in companies.DefaultIfEmpty()
                where u.Email == request.Petition.Email
                select new { User = u, Company = c };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);

            if (userData?.User == null)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: User not found",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            var user = userData.User;
            var company = userData.Company;

            // 2. Verificar contraseña
            if (!_passwordHasher.Verify(request.Petition.Password, user.Password))
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: Invalid password",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // 3. Verificar estado del usuario
            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            if (user.Confirm != true)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: Account not confirmed",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Please confirm your account first"
                );
            }

            // 4. Obtener roles del usuario
            var rolesQuery =
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == user.Id
                select r;

            var roles = await rolesQuery.ToListAsync(cancellationToken);
            var roleNames = roles.Select(r => r.Name).ToList();

            // 5. Verificar autorización para Staff portal
            var allowedPortals = roles.Select(r => r.PortalAccess).Distinct().ToList();
            bool allowed =
                allowedPortals.Contains(PortalAccess.Staff)
                || allowedPortals.Contains(PortalAccess.Developer);

            if (!allowed)
            {
                _logger.LogWarning(
                    "Login failed for user {Email}: Not authorized for Staff portal. Roles: {Roles}",
                    request.Petition.Email,
                    string.Join(",", roleNames)
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "You do not have permission to log in here."
                )
                {
                    StatusCode = 403,
                };
            }

            // 6. Obtener permisos del usuario
            var permissionsQuery =
                from ur in _context.UserRoles
                join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                where ur.TaxUserId == user.Id
                select p.Code;

            var permissionCodes = await permissionsQuery.Distinct().ToListAsync(cancellationToken);
            var portals = allowedPortals.Select(p => p.ToString()).ToList();

            // 7. Crear información del usuario para el token
            var userInfo = new UserInfo(
                UserId: user.Id,
                Email: user.Email,
                Name: user.Name,
                LastName: user.LastName,
                CompanyId: company?.Id ?? Guid.Empty,
                CompanyName: company?.CompanyName,
                CompanyDomain: company?.Domain,
                IsCompany: company?.IsCompany ?? false,
                Roles: roleNames,
                Permissions: permissionCodes,
                Portals: portals
            );

            // 8. Generar tokens
            var sessionId = Guid.NewGuid();
            var sessionInfo = new SessionInfo(sessionId);

            var accessToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(1))
            );
            var refreshToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(7))
            );

            // 9. Crear sesión en base de datos
            var session = new Session
            {
                Id = sessionId,
                TaxUserId = user.Id,
                TaxUser = user,
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            // 10. Publicar evento de login
            var displayName = DetermineDisplayName(
                user.Name,
                user.LastName,
                company?.CompanyName,
                company?.FullName,
                company?.IsCompany ?? false
            );

            var loginEvent = new UserLoginEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                user.Id,
                user.Email,
                user.Name,
                user.LastName,
                DateTime.UtcNow,
                request.IpAddress,
                request.Device,
                company?.Id ?? Guid.Empty,
                company?.FullName,
                company?.CompanyName,
                company?.IsCompany ?? false,
                company?.Domain,
                DateTime.Now.Year
            );

            _eventBus.Publish(loginEvent);

            // 11. Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
            };

            _logger.LogInformation(
                "User {UserId} logged in successfully. Session {SessionId} created",
                user.Id,
                sessionId
            );
            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for {Email}", request.Petition.Email);
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
    }

    private static string DetermineDisplayName(
        string? name,
        string? lastName,
        string? companyName,
        string? fullName,
        bool isCompany
    )
    {
        // Para empresas, usar CompanyName o FullName
        if (isCompany)
        {
            return companyName ?? fullName ?? "Company";
        }

        // Para individuales, usar nombre personal o FullName
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(lastName))
        {
            return $"{name} {lastName}".Trim();
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        // Fallback
        return "User";
    }
}
