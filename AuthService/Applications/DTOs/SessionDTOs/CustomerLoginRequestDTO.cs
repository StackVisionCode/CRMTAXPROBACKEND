namespace AuthService.DTOs.SessionDTOs;

public class CustomerLoginRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
