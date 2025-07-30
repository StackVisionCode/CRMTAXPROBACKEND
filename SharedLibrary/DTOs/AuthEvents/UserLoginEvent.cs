namespace SharedLibrary.DTOs;

public sealed record UserLoginEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string? Name,
    string? LastName,
    DateTime LoginTime,
    string IpAddress,
    string? Device,
    Guid CompanyId,
    string? CompanyFullName,
    string? CompanyName,
    bool IsCompany,
    string? CompanyDomain,
    int Year
) : IntegrationEvent(Id, OccurredOn);
