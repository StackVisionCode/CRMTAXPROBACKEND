namespace SharedLibrary.DTOs.CustomerEventsDTO;

public sealed record CustomerRoleAssignedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CustomerId,
    Guid RoleId // ← RoleId elegido en CustomerService
) : IntegrationEvent(Id, OccurredOn);
