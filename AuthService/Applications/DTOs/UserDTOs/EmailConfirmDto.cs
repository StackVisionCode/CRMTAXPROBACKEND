namespace AuthService.DTOs.UserDTOs;

public class EmailConfirmDto
{
  public required string Email { get; set; }
  public required string Token { get; set; }
}