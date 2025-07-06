using AuthService.Domains.Roles;
using Infraestructure.Context;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;

namespace Applications.EventHandlers.CustomerEventHandlers;

public sealed class CustomerRoleAssignedHandler
    : IIntegrationEventHandler<CustomerRoleAssignedEvent>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerRoleAssignedHandler> _log;

    public CustomerRoleAssignedHandler(
        ApplicationDbContext db,
        ILogger<CustomerRoleAssignedHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task Handle(CustomerRoleAssignedEvent ev)
    {
        bool exists = _db.CustomerRoles.Any(cr =>
            cr.CustomerId == ev.CustomerId && cr.RoleId == ev.RoleId
        );

        if (exists)
            return;

        _db.CustomerRoles.Add(
            new CustomerRole
            {
                Id = Guid.NewGuid(),
                CustomerId = ev.CustomerId,
                RoleId = ev.RoleId,
                CreatedAt = ev.OccurredOn,
            }
        );

        await _db.SaveChangesAsync();

        _log.LogInformation("Asignado rol {RoleId} al cliente {Cust}", ev.RoleId, ev.CustomerId);
    }
}
