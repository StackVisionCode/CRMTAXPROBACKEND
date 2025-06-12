using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Services;

public class CertificateService : ICertificateService
{
    private readonly string _certPath = "certs/mycert.pfx";
    private readonly string _certPassword = "password";

    public string SignFile(string filePath)
    {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        var cert = new X509Certificate2(_certPath, _certPassword, X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
        var data = File.ReadAllBytes(filePath);
        using var rsa = cert.GetRSAPrivateKey();
        var hash = SHA256.HashData(data);
        if (rsa == null)
        {
            throw new InvalidOperationException("The certificate does not have a private key.");
        }
        var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var signaturePath = Path.ChangeExtension(filePath, ".sig");
        File.WriteAllBytes(signaturePath, signature);

        return cert.Thumbprint;
    }
}
