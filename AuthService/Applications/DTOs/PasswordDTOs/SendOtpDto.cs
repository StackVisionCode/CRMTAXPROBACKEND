namespace AuthService.Applications.DTOs;

public class SendOtpDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
}
