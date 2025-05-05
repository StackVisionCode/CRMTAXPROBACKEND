namespace AuthService.DTOs.SessionDTOs;

public class LoginResponseDTO
{
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? RefreshToken { get; set; }
    public required int UserId { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
}