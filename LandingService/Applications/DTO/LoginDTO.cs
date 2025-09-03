namespace LandingService.Applications.DTO;

public class LoginDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool Remember { get; set; }
}   