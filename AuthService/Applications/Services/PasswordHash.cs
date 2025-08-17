using System.Security.Cryptography;
using AuthService.Infraestructure.Services;

namespace AuthService.Applications.Services;

public sealed class PasswordHash : IPasswordHash
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10_000;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);

        byte[] hash = pbkdf2.GetBytes(HashSize);

        // concatenamos   [salt | hash]
        byte[] hashBytes = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        byte[] hashBytes = Convert.FromBase64String(hash);

        byte[] salt = new byte[SaltSize];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);

        byte[] hashToCompare = pbkdf2.GetBytes(HashSize);

        for (int i = 0; i < HashSize; i++)
            if (hashBytes[i + SaltSize] != hashToCompare[i])
                return false;

        return true;
    }
}
