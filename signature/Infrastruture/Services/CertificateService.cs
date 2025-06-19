using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Application.Interfaces;

namespace Infrastructure.Services;

public class CertificateService : ICertificateService
{
    private readonly string _certPath = "certs/firma_taxprotech.pfx";
    private readonly string _certPassword = "TaxPro2025!";

    public string SignFile(string filePath)
    {

        // Carga el primer certificado con clave privada del PFX
        var cert = X509CertificateLoader.LoadPkcs12FromFile(
            _certPath,             // ruta al .pfx
            _certPassword,         // contraseña
            X509KeyStorageFlags.Exportable
        );

        // Leer el archivo que se va a firmar
        var data = File.ReadAllBytes(filePath);

        // Obtener la clave privada del certificado
        using var rsa = cert.GetRSAPrivateKey();

        // Hashear el archivo con SHA256
        var hash = SHA256.HashData(data);

        // Firmar el hash
        var signature = rsa!.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Guardar la firma en un archivo .sig
        var signaturePath = Path.ChangeExtension(filePath, ".sig");
        File.WriteAllBytes(signaturePath, signature);

        // Retornar el Thumbprint del certificado (como referencia)
        return cert.Thumbprint;
    }
}
