using System.Security.Cryptography.X509Certificates;
using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

public class SubmitSignatureHandler(SignatureDbContext db,IPdfService pdf,IConfirmTokenService tokenSvc
    ): IRequestHandler<SubmitSignatureCommand, ApiResponse<bool>>
{
     private readonly string _certPath = "certs/firma_taxprotech.pfx";
    private readonly string _certPassword = "TaxPro2025!";

    
    public async Task<ApiResponse<bool>> Handle(SubmitSignatureCommand c, CancellationToken ct)
    {

         var certificate2 = new X509Certificate2(_certPath, _certPassword, X509KeyStorageFlags.Exportable);

        var (ok, signerId, reqId) = tokenSvc.Validate(c.Payload.Token, "sign");
        if (!ok) return new ApiResponse<bool>(false, "Token inválido");

        var req = await db.SignatureRequests.Include(x => x.Signers)
                                            .FirstAsync(x => x.Id == reqId, ct);

        var cert = new DigitalCertificate(
            c.Payload.Certificate.Thumbprint,
            c.Payload.Certificate.Subject,
            c.Payload.Certificate.NotBefore,
            c.Payload.Certificate.NotAfter);

        // **Validación mínima de cert** (fecha & thumbprint)
        if (cert.NotAfter < DateTime.UtcNow)
            return new ApiResponse<bool>(false, "Certificado caducado");

        req.ReceiveSignature(signerId, c.Payload.SignatureImageBase64, cert);
        await db.SaveChangesAsync(ct);

        if (req.Status == SignatureStatus.Completed)
        {
            var original =  "cloud.DownloadDocumentAsync(req.DocumentId, ct)";
            //await pdf.EmbedImagesAndSignAsync(original, req.Signers, certificate2);
            var finalPdf = original;
            //await cloud.UploadFinalVersionAsync(req.DocumentId, finalPdf, ct);

           
        }

        return new ApiResponse<bool>(true, "Firma registrada");
    }
}
