namespace SharedLibrary.DTOs;

public sealed record UserLoginEvent(
    Guid Id,
    DateTime OccurredOn,
    int UserId,
    string Email,
    string FullName,
    DateTime LoginTime,
    string IpAddress,
    string Device,
    int CompanyId)
  : IntegrationEvent(Id, OccurredOn);