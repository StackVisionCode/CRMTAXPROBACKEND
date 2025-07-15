using AuthService.Domains.Sessions;
using AuthService.DTOs.SessionDTOs;
using Commands.CustomerCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs;

namespace Handlers.CustomerHandlers;

public class CustomerLoginHandler
    : IRequestHandler<CustomerLoginCommand, ApiResponse<LoginResponseDTO>>
{
    private readonly IHttpClientFactory _http;
    private readonly IPasswordHash _hash;
    private readonly ITokenService _token;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerLoginHandler> _log;
    private readonly IEventBus _bus;

    public CustomerLoginHandler(
        IHttpClientFactory http,
        IPasswordHash hash,
        ITokenService token,
        ApplicationDbContext db,
        ILogger<CustomerLoginHandler> log,
        IEventBus bus
    )
    {
        _http = http;
        _hash = hash;
        _token = token;
        _db = db;
        _log = log;
        _bus = bus;
    }

    public async Task<ApiResponse<LoginResponseDTO>> Handle(
        CustomerLoginCommand req,
        CancellationToken ct
    )
    {
        try
        {
            // 1) llamar a CustomerService para obtener AuthInfoDTO
            var client = _http.CreateClient("Customers");
            var resp = await client.GetAsync(
                $"/api/ContactInfo/Internal/AuthInfo?email={req.Petition.Email}",
                ct
            );

            if (!resp.IsSuccessStatusCode)
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            var wrapper = await resp.Content.ReadFromJsonAsync<ApiResponse<RemoteAuthInfoDTO>>(
                cancellationToken: ct
            );

            var data = wrapper?.Data;
            if (data is null || !data.IsLogin)
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            // 2) verificar contraseña
            if (!_hash.Verify(req.Petition.Password, data.PasswordHash))
                return new ApiResponse<LoginResponseDTO>(false, "Invalid credentials");

            /* ------------------------------------------------------------------
             * Cargamos roles y permisos reales del cliente
             * -----------------------------------------------------------------*/
            var roleNames = await (
                from cr in _db.CustomerRoles
                where cr.CustomerId == data.CustomerId
                join r in _db.Roles on cr.RoleId equals r.Id
                select r.Name
            ).ToListAsync(ct);

            if (!roleNames.Any())
                roleNames.Add("Customer");

            var portals = await _db
                .Roles.Where(r => roleNames.Contains(r.Name))
                .Select(r => r.PortalAccess.ToString()) // enum → string
                .Distinct()
                .ToListAsync(ct);

            if (!portals.Any())
                portals.Add("Customer");

            var permCodes = await (
                from cr in _db.CustomerRoles
                where cr.CustomerId == data.CustomerId
                join rp in _db.RolePermissions on cr.RoleId equals rp.RoleId
                join p in _db.Permissions on rp.PermissionId equals p.Id
                select p.Code
            )
                .Distinct()
                .ToListAsync(ct);

            bool allowed = await _db
                .Roles.Where(r => roleNames.Contains(r.Name))
                .AnyAsync(
                    r =>
                        r.PortalAccess == PortalAccess.Customer
                        || r.PortalAccess == PortalAccess.Both,
                    ct
                );

            if (!allowed)
            {
                _log.LogWarning(
                    "Role(s) {Roles} not authorized for Customer login",
                    string.Join(',', roleNames)
                );

                return new ApiResponse<LoginResponseDTO>(false, "Exclusive portal for clients")
                {
                    StatusCode = 403,
                };
            }

            // 3) generar token
            var sessionId = Guid.NewGuid();
            var userInfo = new UserInfo(
                UserId: data.CustomerId,
                Email: data.Email,
                Name: data.DisplayName, // ⟵ para clientes guardamos DisplayName en Name
                LastName: null,
                Address: null,
                PhotoUrl: null,
                CompanyId: Guid.Empty,
                CompanyName: null,
                FullName: null,
                CompanyBrand: null,
                Roles: roleNames,
                Permissions: permCodes,
                Portals: portals
            );

            var sessionInfo = new SessionInfo(sessionId);
            var access = _token.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(1))
            );
            var refresh = _token.Generate(
                new TokenGenerationRequest(userInfo, sessionInfo, TimeSpan.FromDays(3))
            );

            // 4) persistir sesión
            var s = new CustomerSession
            {
                Id = sessionId,
                CustomerId = data.CustomerId,
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
                IpAddress = req.IpAddress,
                Device = req.Device,
                IsRevoke = false,
                CreatedAt = DateTime.UtcNow,
            };

            _db.CustomerSessions.Add(s);
            await _db.SaveChangesAsync(ct);

            var result = new LoginResponseDTO
            {
                TokenRequest = access.AccessToken,
                ExpireTokenRequest = access.ExpireAt,
                TokenRefresh = refresh.AccessToken,
            };

            _log.LogInformation("Login correcto: {Email}", req.Petition.Email);

            return new ApiResponse<LoginResponseDTO>(true, "Login correcto", result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al iniciar sesión: {Message}", ex.Message);
            return new ApiResponse<LoginResponseDTO>(false, ex.Message, null!);
        }
    }
}
