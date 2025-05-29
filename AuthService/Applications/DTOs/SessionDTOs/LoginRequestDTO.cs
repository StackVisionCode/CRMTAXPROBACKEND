namespace AuthService.DTOs.SessionDTOs;

public class LoginRequestDTO
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
}