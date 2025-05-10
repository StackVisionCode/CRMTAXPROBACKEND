public interface ITokenStorage
{
    void StoreToken(AuthEventRequest authData);
    string? GetToken(AuthEventRequest authData);
}