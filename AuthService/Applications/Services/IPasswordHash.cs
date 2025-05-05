namespace AuthService.Applications.Services
{
    public interface IPasswordHash
    {
        string HasPassword(string password);
        bool Verify(string password, string hash);
    }
}