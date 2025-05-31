namespace SharedLibrary.DTOs;

public sealed record UserLoginEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string Name,
    string LastName,
    DateTime LoginTime,
    string IpAddress,
    string Device,
    Guid CompanyId,
    string CompanyName,
    string FullName,
    string DisplayName,
    int Year
) : IntegrationEvent(Id, OccurredOn);
