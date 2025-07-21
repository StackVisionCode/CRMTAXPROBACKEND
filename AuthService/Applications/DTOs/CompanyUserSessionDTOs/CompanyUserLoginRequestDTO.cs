namespace AuthService.DTOs.CompanyUserSessionDTOs;

public class CompanyUserLoginRequestDTO
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
}
