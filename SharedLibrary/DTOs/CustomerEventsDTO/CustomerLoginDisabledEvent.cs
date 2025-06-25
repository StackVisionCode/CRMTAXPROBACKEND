namespace SharedLibrary.DTOs.CustomerEventsDTO;

public sealed record CustomerLoginDisabledEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CustomerId,
    string DisplayName,
    string Email
) : IntegrationEvent(Id, OccurredOn);
