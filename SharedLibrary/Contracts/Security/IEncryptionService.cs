namespace SharedLibrary.Contracts.Security;

public interface IEncryptionService
{
    string Encrypt(object data, string? recipientKey = null);
    T Decrypt<T>(string encryptedData, string? senderKey = null);
    string GenerateKey();
    bool ValidateKey(string key);
}
