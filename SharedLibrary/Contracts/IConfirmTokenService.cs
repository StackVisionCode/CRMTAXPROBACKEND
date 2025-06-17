namespace SharedLibrary.Contracts;

public interface IConfirmTokenService
{
  (string Token, DateTime Expires) Generate(Guid uid, string email);
  (bool IsValid, Guid SignerId, Guid RequestId) Validate(string token, string expectedPurpose);
}