namespace LandingService.Applications.DTO;

public class EmailConfirmDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
}
