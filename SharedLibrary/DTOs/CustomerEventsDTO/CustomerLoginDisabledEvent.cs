namespace SharedLibrary.DTOs.CustomerEventsDTO;

public sealed record CustomerLoginDisabledEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CustomerId,
    string Email,
    string DisplayName
) : IntegrationEvent(Id, OccurredOn);
