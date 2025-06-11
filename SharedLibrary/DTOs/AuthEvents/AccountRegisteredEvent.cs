namespace SharedLibrary.DTOs.AuthEvents;

public sealed record AccountRegisteredEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string Name,
    string LastName,
    string Phone,
    bool IsCompany,
    Guid? CompanyId,
    string? FullName,
    string? CompanyName,
    string? Domain
) : IntegrationEvent(Id, OccurredOn);
