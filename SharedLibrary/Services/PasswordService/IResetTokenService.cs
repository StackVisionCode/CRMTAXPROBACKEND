namespace SharedLibrary.Services;

public interface IResetTokenService
{
    (string Token, DateTime Expires) Generate(Guid userId, string email);
}
