namespace SharedLibrary.DTOs;

public sealed record AccountConfirmedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CompanyId,
    string? Name,
    string? LastName,
    string? FullName,
    string? CompanyName,
    string? Domain,
    bool IsCompany,
    Guid UserId,
    string Email
) : IntegrationEvent(Id, OccurredOn);
