namespace AuthService.Infraestructure.Services;

public interface ITokenService
{
    (string accessToken, DateTime expiry) GenerateAccessToken(int userId, string email, string name, TimeSpan lifeTime);
    bool ValidateToken(string token);
    int GetUserIdFromToken(string token);
    string GetEmailFromToken(string token);
}