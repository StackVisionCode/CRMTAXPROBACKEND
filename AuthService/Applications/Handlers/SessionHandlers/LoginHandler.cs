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
            // 🔍 PASO 1: Buscar usuario en ambas tablas simultáneamente
            var userResult = await FindUserByEmailAsync(request.Petition.Email, cancellationToken);

            if (userResult == null)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: User not found",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // 🔒 PASO 2: Verificar contraseña
            if (!_passwordHasher.Verify(request.Petition.Password, userResult.HashedPassword))
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Invalid password",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");
            }

            // ✅ PASO 3: Verificar estado del usuario
            if (!userResult.IsActive)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: User is inactive",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(false, "User account is inactive");
            }

            if (!userResult.IsConfirmed)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Account not confirmed",
                    request.Petition.Email
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "Please confirm your account first"
                );
            }

            // 🎭 PASO 4: Obtener roles y permisos según el tipo de usuario
            var rolesAndPermissions = await GetUserRolesAndPermissionsAsync(
                userResult.UserId,
                userResult.UserType,
                cancellationToken
            );

            // 🚪 PASO 5: Verificar autorización para Staff portal
            var allowedPortals = rolesAndPermissions
                .Roles.Select(r => r.PortalAccess)
                .Distinct()
                .ToList();
            bool allowed =
                allowedPortals.Contains(PortalAccess.Staff)
                || allowedPortals.Contains(PortalAccess.Developer)
                || allowedPortals.Contains(PortalAccess.Both);

            if (!allowed)
            {
                _logger.LogWarning(
                    "Login failed for {Email}: Not authorized for Staff portal. Type: {UserType}, Roles: {Roles}",
                    request.Petition.Email,
                    userResult.UserType,
                    string.Join(",", rolesAndPermissions.Roles.Select(r => r.Name))
                );
                return new ApiResponse<LoginResponseDTO>(
                    false,
                    "You do not have permission to log in here."
                )
                {
                    StatusCode = 403,
                };
            }

            // 🎫 PASO 6: Crear información para el token
            var userInfo = new UserInfo(
                UserId: userResult.UserId,
                Email: userResult.Email,
                Name: userResult.Name,
                LastName: userResult.LastName,
                CompanyId: userResult.CompanyId,
                CompanyName: userResult.CompanyName,
                CompanyDomain: userResult.CompanyDomain,
                IsCompany: userResult.IsCompany,
                Roles: rolesAndPermissions.Roles.Select(r => r.Name),
                Permissions: rolesAndPermissions.Permissions.Select(p => p.Code),
                Portals: allowedPortals.Select(p => p.ToString()).ToList()
            );

            // 🎟️ PASO 7: Generar tokens
            var sessionId = Guid.NewGuid();
            var sessionInfo = new SessionInfo(sessionId);

            var accessTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(30)
                : TimeSpan.FromDays(1);
            var refreshTokenLifetime = request.Petition.RememberMe
                ? TimeSpan.FromDays(90)
                : TimeSpan.FromDays(7);

            var accessToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, accessTokenLifetime)
            );
            var refreshToken = _tokenService.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, refreshTokenLifetime)
            );

            // 💾 PASO 8: Crear sesión en la tabla correspondiente
            await CreateUserSessionAsync(
                userResult,
                sessionId,
                accessToken,
                refreshToken,
                request,
                cancellationToken
            );

            // 📧 PASO 9: Publicar evento de login
            var displayName = DetermineDisplayName(userResult);
            var loginEvent = new UserLoginEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                userResult.UserId,
                userResult.Email,
                userResult.Name,
                userResult.LastName,
                DateTime.UtcNow,
                request.IpAddress,
                request.Device,
                userResult.CompanyId,
                userResult.CompanyFullName,
                userResult.CompanyName,
                userResult.IsCompany,
                userResult.CompanyDomain,
                DateTime.Now.Year
            );

            _eventBus.Publish(loginEvent);

            // 📋 PASO 10: Preparar respuesta
            var response = new LoginResponseDTO
            {
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
            };

            _logger.LogInformation(
                "User {UserId} ({UserType}) logged in successfully. Session {SessionId} created",
                userResult.UserId,
                userResult.UserType,
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

    /// <summary>
    /// 🔍 Busca usuario en ambas tablas (TaxUsers y UserCompanies)
    /// </summary>
    private async Task<UserLoginResult?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        // Buscar en TaxUsers primero
        var taxUserQuery =
            from u in _context.TaxUsers
            join c in _context.Companies on u.CompanyId equals c.Id
            where u.Email == email
            select new UserLoginResult
            {
                UserId = u.Id,
                Email = u.Email,
                HashedPassword = u.Password,
                Name = u.Name,
                LastName = u.LastName,
                IsActive = u.IsActive,
                IsConfirmed = u.Confirm ?? false,
                CompanyId = c.Id,
                CompanyName = c.CompanyName,
                CompanyFullName = c.FullName,
                CompanyDomain = c.Domain,
                IsCompany = c.IsCompany,
                UserType = "TaxUser",
            };

        var taxUser = await taxUserQuery.FirstOrDefaultAsync(ct);
        if (taxUser != null)
            return taxUser;

        // Si no se encuentra en TaxUsers, buscar en UserCompanies
        var userCompanyQuery =
            from uc in _context.UserCompanies
            join c in _context.Companies on uc.CompanyId equals c.Id
            where uc.Email == email
            select new UserLoginResult
            {
                UserId = uc.Id,
                Email = uc.Email,
                HashedPassword = uc.Password,
                Name = uc.Name,
                LastName = uc.LastName,
                IsActive = uc.IsActive,
                IsConfirmed = uc.Confirm ?? false,
                CompanyId = c.Id,
                CompanyName = c.CompanyName,
                CompanyFullName = c.FullName,
                CompanyDomain = c.Domain,
                IsCompany = c.IsCompany,
                UserType = "UserCompany",
            };

        return await userCompanyQuery.FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 🎭 Obtiene roles y permisos según el tipo de usuario
    /// </summary>
    private async Task<(
        List<RoleResult> Roles,
        List<PermissionResult> Permissions
    )> GetUserRolesAndPermissionsAsync(Guid userId, string userType, CancellationToken ct)
    {
        if (userType == "TaxUser")
        {
            // Roles de TaxUser
            var taxUserRoles = await (
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == userId
                select new RoleResult
                {
                    Id = r.Id,
                    Name = r.Name,
                    PortalAccess = r.PortalAccess,
                }
            ).ToListAsync(ct);

            // Permisos de TaxUser
            var taxUserPermissions = await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions on ur.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                where ur.TaxUserId == userId
                select new PermissionResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                }
            ).Distinct().ToListAsync(ct);

            return (taxUserRoles, taxUserPermissions);
        }
        else // UserCompany
        {
            // Roles de UserCompany
            var userCompanyRoles = await (
                from ucr in _context.UserCompanyRoles
                join r in _context.Roles on ucr.RoleId equals r.Id
                where ucr.UserCompanyId == userId
                select new RoleResult
                {
                    Id = r.Id,
                    Name = r.Name,
                    PortalAccess = r.PortalAccess,
                }
            ).ToListAsync(ct);

            // Permisos de roles
            var rolePermissions = await (
                from ucr in _context.UserCompanyRoles
                join rp in _context.RolePermissions on ucr.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.Id
                where ucr.UserCompanyId == userId
                select new PermissionResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                }
            ).ToListAsync(ct);

            // Permisos personalizados granted
            var customPermissions = await (
                from cp in _context.CompanyPermissions
                where cp.UserCompanyId == userId && cp.IsGranted
                select new PermissionResult
                {
                    Id = Guid.NewGuid(),
                    Name = cp.Name,
                    Code = cp.Code,
                }
            ).ToListAsync(ct);

            // Permisos revocados
            var revokedPermissions = await (
                from cp in _context.CompanyPermissions
                where cp.UserCompanyId == userId && !cp.IsGranted
                select cp.Code
            ).ToListAsync(ct);

            // Combinar permisos: (roles + custom) - revocados
            var allPermissions = rolePermissions
                .Concat(customPermissions)
                .Where(p => !revokedPermissions.Contains(p.Code))
                .DistinctBy(p => p.Code)
                .ToList();

            return (userCompanyRoles, allPermissions);
        }
    }

    /// <summary>
    /// 💾 Crea sesión en la tabla correspondiente según el tipo de usuario
    /// </summary>
    private async Task CreateUserSessionAsync(
        UserLoginResult user,
        Guid sessionId,
        TokenResult accessToken,
        TokenResult refreshToken,
        LoginCommands request,
        CancellationToken ct
    )
    {
        if (user.UserType == "TaxUser")
        {
            var session = new Session
            {
                Id = sessionId,
                TaxUserId = user.UserId,
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Sessions.Add(session);
        }
        else // UserCompany
        {
            var session = new UserCompanySession
            {
                Id = sessionId,
                UserCompanyId = user.UserId,
                TokenRequest = accessToken.AccessToken,
                ExpireTokenRequest = accessToken.ExpireAt,
                TokenRefresh = refreshToken.AccessToken,
                IpAddress = request.IpAddress,
                Device = request.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _context.UserCompanySessions.Add(session);
        }

        await _context.SaveChangesAsync(ct);
    }

    private string DetermineDisplayName(UserLoginResult user)
    {
        if (user.IsCompany && !string.IsNullOrWhiteSpace(user.CompanyName))
        {
            return user.CompanyName;
        }

        if (!user.IsCompany && !string.IsNullOrWhiteSpace(user.CompanyFullName))
        {
            return user.CompanyFullName;
        }

        if (!string.IsNullOrWhiteSpace(user.Name) || !string.IsNullOrWhiteSpace(user.LastName))
        {
            return $"{user.Name} {user.LastName}".Trim();
        }

        return user.Email;
    }
}

// 📊 Clases auxiliares
public class UserLoginResult
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfirmed { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyFullName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool IsCompany { get; set; }
    public required string UserType { get; set; }
}

public class RoleResult
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public PortalAccess PortalAccess { get; set; }
}

public class PermissionResult
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
}
