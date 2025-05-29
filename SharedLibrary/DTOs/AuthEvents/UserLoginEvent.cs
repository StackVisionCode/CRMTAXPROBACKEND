namespace SharedLibrary.DTOs;

public sealed record UserLoginEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string FullName,
    string V,
    DateTime LoginTime,
    string IpAddress,
    string Device,
    Guid CompanyId)
  : IntegrationEvent(Id, OccurredOn);