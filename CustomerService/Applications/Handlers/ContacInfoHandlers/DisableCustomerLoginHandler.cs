using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class DisableCustomerLoginHandler
    : IRequestHandler<DisableCustomerLoginCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly IEventBus _bus;
    private readonly ILogger<DisableCustomerLoginHandler> _log;

    public DisableCustomerLoginHandler(
        ApplicationDbContext db,
        IEventBus bus,
        ILogger<DisableCustomerLoginHandler> log
    )
    {
        _db = db;
        _bus = bus;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        DisableCustomerLoginCommand cmd,
        CancellationToken ct
    )
    {
        try
        {
            var cust = await _db
                .Customers.Include(c => c.Contact)
                .FirstOrDefaultAsync(c => c.Id == cmd.CustomerId, ct);

            if (cust is null || cust.Contact is null)
                return new ApiResponse<bool>(false, "Cliente no encontrado", false);

            if (!cust.Contact.IsLoggin)
                return new ApiResponse<bool>(false, "Ya estaba deshabilitado", false);

            cust.Contact.IsLoggin = false;
            _db.Update(cust);
            await _db.SaveChangesAsync(ct);

            _bus.Publish(
                new CustomerLoginDisabledEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    cust.Id,
                    cust.Contact.Email,
                    $"{cust.FirstName} {cust.LastName}".Trim()
                )
            );

            _log.LogInformation("Login deshabilitado para cliente {Id}", cust.Id);
            return new ApiResponse<bool>(true, "Login deshabilitado", true);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al deshabilitar login para cliente {Id}", cmd.CustomerId);
            return new ApiResponse<bool>(false, "Error al deshabilitar login", false);
        }
    }
}
