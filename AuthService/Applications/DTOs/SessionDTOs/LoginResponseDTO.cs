namespace AuthService.DTOs.SessionDTOs;

public class LoginResponseDTO
{
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public required string TokenRefresh { get; set; } 
}