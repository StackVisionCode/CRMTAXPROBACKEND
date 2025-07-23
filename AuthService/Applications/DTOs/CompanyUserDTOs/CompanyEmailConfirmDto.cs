namespace AuthService.DTOs.CompanyUserDTOs;

public class CompanyEmailConfirmDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
}
