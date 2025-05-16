

using Infraestructure.Commands.Signatures;
using iText.Commons.Bouncycastle.Crypto;
using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using iText.Signatures;
using MediatR;
using Org.BouncyCastle.Security;
using System.Security.Cryptography.X509Certificates;

namespace Applications.Handlers.Signatures;

public class SignPdfCommandHandler : IRequestHandler<SignPdfCommand, string>
{

 public async Task<string> Handle(SignPdfCommand request, CancellationToken cancellationToken)
    {
        var certPath = Path.Combine("Service", "Ca", "user.pfx");
        var inputPath = Path.Combine("wwwroot", "Documents", request.InputFileName);
        var outputPath = Path.Combine("wwwroot", "Signed", request.OutputFileName);

        if (!File.Exists(certPath) || !File.Exists(inputPath))
            throw new FileNotFoundException("Certificado o PDF no encontrado.");

        var cert = new X509Certificate2(certPath, "123", X509KeyStorageFlags.Exportable);
        //var bcCert = DotNetUtilities.FromX509Certificate(cert);
       // var keyPair = DotNetUtilities.GetKeyPair(cert.PrivateKey);

        using var reader = new PdfReader(inputPath);
        using var output = new FileStream(outputPath, FileMode.Create);

        // Crear signer con propiedades configuradas
        var signerProps = new SignerProperties()
            .SetFieldName("Signature1") // El nombre del campo de firma (puede ser nuevo)
            .SetReason("Firma Digital")
            .SetLocation("Santo Domingo")
            .SetPageNumber(1)
            .SetPageRect(new iText.Kernel.Geom.Rectangle(100, 100, 200, 100)); // Posición y tamaño

        var digest = new BouncyCastleDigest();
        //var signature = new PrivateKeySignature(keyPair.Private, DigestAlgorithms.SHA256);

       // ISigner.SignDetached(digest, signature, new[] { bcCert }, null, null, null, 0, PdfSigner.CryptoStandard.CADES);

        return outputPath;
    }

}
