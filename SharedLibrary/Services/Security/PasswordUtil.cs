using System.Security.Cryptography;
using System.Text;

namespace SharedLibrary.Services.Security;

/// <summary>Utilidades relativas a contraseñas.</summary>
public static class PasswordUtil
{
    private const string Allowed =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@$!%*?&";

    /// <summary>Genera una contraseña aleatoria criptográficamente segura.</summary>
    public static string GenerateSecure(int length = 16)
    {
        var sb = new StringBuilder(length);
        byte[] bytes = RandomNumberGenerator.GetBytes(length);

        for (int i = 0; i < length; i++)
        {
            sb.Append(Allowed[bytes[i] % Allowed.Length]);
        }
        return sb.ToString();
    }
}
