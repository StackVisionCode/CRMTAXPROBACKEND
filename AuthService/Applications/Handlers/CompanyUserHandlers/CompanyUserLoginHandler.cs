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

namespace Handlers.CompanyUserHandlers;

public class CompanyUserLoginHandler
    : IRequestHandler<CompanyUserLoginCommand, ApiResponse<LoginResponseDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHash _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<CompanyUserLoginHandler> _logger;

    public CompanyUserLoginHandler(
        ApplicationDbContext context,
        IPasswordHash passwordHasher,
        ITokenService tokenService,
        ILogger<CompanyUserLoginHandler> logger
    )
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponseDTO>> Handle(
        CompanyUserLoginCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. JOIN EXPLÍCITO: Obtener datos completos del usuario con una sola consulta
            var userLoginData = await (
                from cu in _context.CompanyUsers
                join cup in _context.CompanyUserProfiles on cu.Id equals cup.CompanyUserId
                join c in _context.Companies on cu.CompanyId equals c.Id
                where cu.Email == request.Petition.Email
                select new
                {
                    // Datos del usuario
                    UserId = cu.Id,
                    Email = cu.Email,
                    Password = cu.Password,
                    IsActive = cu.IsActive,
                    CompanyId = cu.CompanyId,

                    // Datos del perfil
                    Name = cup.Name,
                    LastName = cup.LastName,
                    Address = cup.Address,
                    PhotoUrl = cup.PhotoUrl,
                    Position = cup.Position,

                    // Datos de la empresa
                    CompanyName = c.CompanyName,
                    CompanyFullName = c.FullName,
                    CompanyBrand = c.Brand,
                    UserLimit = c.UserLimit,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (userLoginData is null)
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!_passwordHasher.Verify(request.Petition.Password, userLoginData.Password))
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!userLoginData.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            // 2. JOIN EXPLÍCITO: Verificar límite de usuarios activos de la empresa
            var activeUsersCount = await (
                from cus in _context.CompanyUserSessions
                join cu in _context.CompanyUsers on cus.CompanyUserId equals cu.Id
                where
                    cu.CompanyId == userLoginData.CompanyId
                    && !cus.IsRevoke
                    && cus.ExpireTokenRequest > DateTime.UtcNow
                select cus.CompanyUserId
            )
                .Distinct()
                .CountAsync(cancellationToken);

            if (activeUsersCount >= userLoginData.UserLimit)
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: Company user limit reached",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Company user limit reached. Contact your administrator."
                );
            }

            // 3. JOIN EXPLÍCITO: Obtener roles del usuario
            var roleNames = await (
                from cur in _context.CompanyUserRoles
                join r in _context.Roles on cur.RoleId equals r.Id
                where cur.CompanyUserId == userLoginData.UserId
                select r.Name
            ).ToListAsync(cancellationToken);

            // 4. JOIN EXPLÍCITO: Obtener portales disponibles para los roles
            var portals = await (
                from cur in _context.CompanyUserRoles
                join r in _context.Roles on cur.RoleId equals r.Id
                where cur.CompanyUserId == userLoginData.UserId
                select r.PortalAccess.ToString()
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // 5. JOIN EXPLÍCITO: Obtener permisos del usuario
            var permCodes = await (
                from cur in _context.CompanyUserRoles
                join rp in _context.RolePermissions on cur.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                where cur.CompanyUserId == userLoginData.UserId
                select p.Code
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // 6. JOIN EXPLÍCITO: Verificar acceso al portal
            var hasStaffAccess = await (
                from cur in _context.CompanyUserRoles
                join r in _context.Roles on cur.RoleId equals r.Id
                where
                    cur.CompanyUserId == userLoginData.UserId
                    && (r.PortalAccess == PortalAccess.Staff || r.PortalAccess == PortalAccess.Both)
                select r.Id
            ).AnyAsync(cancellationToken);

            if (!hasStaffAccess)
            {
                _logger.LogWarning(
                    "Role {Roles} not authorized for Company User login",
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

            // 7. Generar tokens
            var sessionId = Guid.NewGuid();

            var userInfo = new UserInfo(
                userLoginData.UserId,
                userLoginData.Email,
                userLoginData.Name ?? string.Empty,
                userLoginData.LastName ?? string.Empty,
                userLoginData.Address ?? string.Empty,
                userLoginData.PhotoUrl ?? string.Empty,
                userLoginData.CompanyId,
                userLoginData.CompanyName ?? string.Empty,
                userLoginData.CompanyFullName ?? string.Empty,
                userLoginData.CompanyBrand ?? string.Empty,
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

            // 8. Crear sesión - SIN navegación, solo IDs
            var session = new CompanyUserSession
            {
                Id = sessionId,
                CompanyUserId = userLoginData.UserId, // Solo el ID, no la navegación
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.CompanyUserSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            // 9. Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _logger.LogInformation(
                "Company user {Id} logged-in successfully. Session {SessionId} created",
                userLoginData.UserId,
                session.Id
            );
            return new ApiResponse<LoginResponseDTO>(true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during company user login process for {Email}",
                request.Petition.Email
            );
            return new ApiResponse<LoginResponseDTO>(false, "An error occurred during login");
        }
    }
}
