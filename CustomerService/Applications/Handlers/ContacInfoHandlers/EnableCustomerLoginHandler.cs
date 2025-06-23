using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.DTOs.CustomerEventsDTO;
using SharedLibrary.Services.Security;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class EnableCustomerLoginHandler
    : IRequestHandler<EnableCustomerLoginCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHash _hash;
    private readonly IEventBus _bus;
    private readonly ILogger<EnableCustomerLoginHandler> _logger;

    public EnableCustomerLoginHandler(
        ApplicationDbContext db,
        IPasswordHash hash,
        IEventBus bus,
        ILogger<EnableCustomerLoginHandler> logger
    )
    {
        _db = db;
        _hash = hash;
        _bus = bus;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        EnableCustomerLoginCommand req,
        CancellationToken ct
    )
    {
        try
        {
            var dto = req.Data;

            var cust = await _db
                .Customers.Include(c => c.Contact)
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId, ct);

            if (cust is null || cust.Contact is null)
                return new ApiResponse<bool>(false, "Cliente no encontrado", false);

            // Habilitar
            if (dto.Enable)
            {
                if (cust.Contact.IsLoggin)
                    return new ApiResponse<bool>(false, "Ya estaba habilitado", false);

                string plain = string.IsNullOrWhiteSpace(dto.Password)
                    ? PasswordUtil.GenerateSecure()
                    : dto.Password!;

                cust.Contact.PasswordClient = _hash.HashPassword(plain);
                cust.Contact.IsLoggin = true;
                _db.Update(cust);
                await _db.SaveChangesAsync(ct);

                // Publicar evento
                _bus.Publish(
                    new CustomerLoginEnabledEvent(
                        Guid.NewGuid(),
                        DateTime.UtcNow,
                        cust.Id,
                        cust.Contact.Email,
                        $"{cust.FirstName} {cust.LastName}".Trim(),
                        plain
                    )
                );
                _logger.LogInformation("Login habilitado para cliente {Id}", cust.Id);
                return new ApiResponse<bool>(true, "Login habilitado", true);
            }
            // Deshabilitar
            else
            {
                cust.Contact.IsLoggin = false;
                _db.Update(cust);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Login deshabilitado para cliente {Id}", cust.Id);
                // Opcional: aquí podrías publicar un evento de deshabilitación o
                // llamar a AuthService para revocar sesiones.
                return new ApiResponse<bool>(true, "Login deshabilitado", true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al habilitar o deshabilitar login del cliente {Id}",
                req.Data.CustomerId
            );
            return new ApiResponse<bool>(false, "Error al habilitar o deshabilitar login", false);
        }
    }
}
