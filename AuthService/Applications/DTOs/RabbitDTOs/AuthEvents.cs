namespace AuthService.Applications.DTOs.RabbitDTOs;

public record LoginEvent(
    int      UserId,
    string   Email,
    string   IpAddress,
    string   Device,
    DateTime LoginTime);