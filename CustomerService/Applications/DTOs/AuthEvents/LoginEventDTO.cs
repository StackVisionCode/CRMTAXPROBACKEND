namespace CustomerService.DTOs.AuthEvents;

public class LoginEventDTO
{
    public required int UserId { get; init; }
    public required string Email { get; init; }
    public required string IpAddress { get; init; }
    public required string Device { get; init; }
}