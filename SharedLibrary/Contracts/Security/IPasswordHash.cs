namespace SharedLibrary.Contracts.Security;

/// <summary>Abstracción de hash seguro de contraseñas.</summary>
public interface IPasswordHash
{
    /// <summary>Devuelve el hash (incluida la sal) de <paramref name="password"/>.</summary>
    string HashPassword(string password);

    /// <summary>Verifica que <paramref name="password"/> coincida con <paramref name="hash"/>.</summary>
    bool Verify(string password, string hash);
}
