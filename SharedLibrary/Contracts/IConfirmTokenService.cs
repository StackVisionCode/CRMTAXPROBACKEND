namespace SharedLibrary.Contracts;

public interface IConfirmTokenService
{
    (string Token, DateTime Expires) Generate(Guid uid, string email);
}
