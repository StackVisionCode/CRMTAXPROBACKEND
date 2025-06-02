namespace SharedLibrary.DTOs.CustomerEventsDTO;

public sealed record CustomerCreatedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CustomerId,
    Guid TaxUserId,
    string FirstName,
    string? MiddleName,
    string? LastName,
    IReadOnlyList<string> Folders
) : IntegrationEvent(Id, OccurredOn);
