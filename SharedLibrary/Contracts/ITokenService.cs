using SharedLibrary.DTOs;

namespace SharedLibrary.Contracts;

public interface ITokenService
{
    TokenResult Generate(TokenGenerationRequest request);
}
