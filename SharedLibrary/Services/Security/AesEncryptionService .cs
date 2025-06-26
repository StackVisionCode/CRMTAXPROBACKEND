using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Security;

namespace SharedLibrary.Services.Security;

public class AesEncryptionService : IEncryptionService
{
    private readonly string _defaultKey;
    private readonly ILogger<AesEncryptionService> _logger;

    public AesEncryptionService(IConfiguration configuration, ILogger<AesEncryptionService> logger)
    {
        _defaultKey =
            configuration["Security:EncryptionKey"]
            ?? throw new InvalidOperationException("Security:EncryptionKey no configurado");
        _logger = logger;

        if (_defaultKey.Length != 32)
            throw new InvalidOperationException(
                "La clave de cifrado debe tener exactamente 32 caracteres"
            );
    }

    public string Encrypt(object data, string? recipientKey = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var key = recipientKey ?? _defaultKey;

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainTextBytes = Encoding.UTF8.GetBytes(json);
            var cipherTextBytes = encryptor.TransformFinalBlock(
                plainTextBytes,
                0,
                plainTextBytes.Length
            );

            // Combinar IV + datos cifrados
            var result = new byte[aes.IV.Length + cipherTextBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(cipherTextBytes, 0, result, aes.IV.Length, cipherTextBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cifrando datos");
            throw new SecurityException("Error en el proceso de cifrado", ex);
        }
    }

    public T Decrypt<T>(string encryptedData, string? senderKey = null)
    {
        try
        {
            var key = senderKey ?? _defaultKey;
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);

            // Extraer IV (primeros 16 bytes)
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, 16);
            aes.IV = iv;

            // Extraer datos cifrados
            var cipherText = new byte[fullCipher.Length - 16];
            Array.Copy(fullCipher, 16, cipherText, 0, cipherText.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainTextBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            var json = Encoding.UTF8.GetString(plainTextBytes);

            return JsonSerializer.Deserialize<T>(json)
                ?? throw new InvalidOperationException("Error deserializando datos descifrados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error descifrando datos");
            throw new SecurityException("Error en el proceso de descifrado", ex);
        }
    }

    public string GenerateKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes)[..32];
    }

    public bool ValidateKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && key.Length == 32;
    }
}
