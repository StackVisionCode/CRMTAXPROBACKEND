namespace SharedLibrary.DTOs.AuthEvents;

public sealed record UserAddedToCompanyEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string? Name,
    string? LastName,
    Guid CompanyId,
    string? CompanyFullName,
    string? CompanyName,
    string? CompanyDomain,
    bool IsCompany,
    IEnumerable<string> Roles
) : IntegrationEvent(Id, OccurredOn);
