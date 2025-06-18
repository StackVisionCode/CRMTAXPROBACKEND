namespace SharedLibrary.Contracts;

public interface ISignatureValidToken
{
    (string Token, DateTime Expires) Generate(Guid signerId, string request, string purpose);

    (bool IsValid, Guid SignerId, string RequestId) Validate(string token, string expected);
}
