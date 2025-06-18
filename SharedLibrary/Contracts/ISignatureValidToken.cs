namespace SharedLibrary.Contracts;

public interface ISignatureValidToken
{
    (string Token, DateTime Expires) Generate(Guid signerId, Guid requestId, string purpose);
    (bool IsValid, Guid SignerId, Guid RequestId) Validate(string token, string expectedPurpose);
}
