using System.Security.Cryptography.X509Certificates;
using Entities;

public interface IPdfService
{
    Task<byte[]> EmbedImagesAndSignAsync(
        byte[] originalPdf,
        IEnumerable<Signer> signers,                  // trae imagen y cert
        X509Certificate2 platformCertificate);       // certificado de la empresa (firma LTV)
}
