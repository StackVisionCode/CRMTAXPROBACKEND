namespace LandingService.Infrastructure.Services
{
    public interface IPasswordHash
    {
        string HashPassword(string password);
        bool Verify(string password, string hash);
    }
}
