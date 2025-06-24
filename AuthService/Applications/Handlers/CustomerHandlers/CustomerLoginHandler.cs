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
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

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
                return new ApiResponse<LoginResponseDTO>(false, "Credenciales inválidas");

            var wrapper = await resp.Content.ReadFromJsonAsync<ApiResponse<RemoteAuthInfoDTO>>(
                cancellationToken: ct
            );

            var data = wrapper?.Data;
            if (data is null || !data.IsLogin)
                return new ApiResponse<LoginResponseDTO>(false, "Credenciales inválidas");

            // 2) verificar contraseña
            if (!_hash.Verify(req.Petition.Password, data.PasswordHash))
                return new ApiResponse<LoginResponseDTO>(false, "Credenciales inválidas");

            // 3) generar token
            var sessionId = Guid.NewGuid();
            var userInfo = new UserInfo(
                data.CustomerId,
                data.Email,
                data.DisplayName, // Name
                string.Empty, // LastName
                null,
                null,
                Guid.Empty,
                null,
                null,
                null
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

            _bus.Publish(
                new UserPresenceChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    data.CustomerId,
                    "Customer",
                    true
                )
            );

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
