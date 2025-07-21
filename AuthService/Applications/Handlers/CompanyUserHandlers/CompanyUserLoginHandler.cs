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
            // 1. Validar credenciales y cargar datos relacionados
            var user = await _context
                .CompanyUsers.Include(u => u.CompanyUserProfile)
                .Include(u => u.Company)
                .Include(u => u.CompanyUserRoles)
                .ThenInclude(cur => cur.Role)
                .FirstOrDefaultAsync(x => x.Email == request.Petition.Email, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!_passwordHasher.Verify(request.Petition.Password, user.Password))
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: Invalid credentials",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for company user {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            // 2. Verificar límite de usuarios activos de la empresa
            var activeUsersCount = await _context
                .CompanyUserSessions.Where(s =>
                    s.CompanyUser.CompanyId == user.CompanyId
                    && !s.IsRevoke
                    && s.ExpireTokenRequest > DateTime.UtcNow
                )
                .Select(s => s.CompanyUserId)
                .Distinct()
                .CountAsync(cancellationToken);

            if (activeUsersCount >= user.Company.UserLimit)
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

            // 3. Preparar datos para el token
            var roleNames = user.CompanyUserRoles.Select(cur => cur.Role.Name).ToList();

            var portals = await _context
                .Roles.Where(r => roleNames.Contains(r.Name))
                .Select(r => r.PortalAccess.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);

            var permCodes = await (
                from cur in _context.CompanyUserRoles
                where cur.CompanyUserId == user.Id
                join rp in _context.RolePermissions on cur.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                select p.Code
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // 4. Verificar acceso al portal
            bool allowed = await _context
                .Roles.Where(r => roleNames.Contains(r.Name))
                .AnyAsync(
                    r =>
                        r.PortalAccess == PortalAccess.Staff || r.PortalAccess == PortalAccess.Both,
                    cancellationToken
                );

            if (!allowed)
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

            // 5. Generar tokens
            var sessionId = Guid.NewGuid();
            var profile = user.CompanyUserProfile;

            var userInfo = new UserInfo(
                user.Id,
                user.Email,
                profile?.Name ?? string.Empty,
                profile?.LastName ?? string.Empty,
                profile?.Address ?? string.Empty,
                profile?.PhotoUrl ?? string.Empty,
                user.CompanyId,
                user.Company?.CompanyName ?? string.Empty,
                user.Company?.FullName ?? string.Empty,
                user.Company?.Brand ?? string.Empty,
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

            // 6. Crear sesión
            var session = new CompanyUserSession
            {
                Id = sessionId,
                CompanyUser = user,
                CompanyUserId = user.Id,
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

            // 7. Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _logger.LogInformation(
                "Company user {Id} logged-in successfully. Session {SessionId} created",
                user.Id,
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
