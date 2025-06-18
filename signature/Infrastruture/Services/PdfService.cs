using System.Security.Cryptography.X509Certificates;
using Entities;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

public class PdfService : IPdfService
{
    public async Task<byte[]> EmbedImagesAndSignAsync(
        byte[] pdf,
        IEnumerable<Signer> signers,
        X509Certificate2 platformCert
    )
    {
        using var src = new MemoryStream(pdf);
        using var dest = new MemoryStream();

        var reader = new PdfReader(src);
        var writer = new PdfWriter(dest, new WriterProperties().UseSmartMode());
        var pdfDoc = new PdfDocument(reader, writer);

        // ① insertar imágenes
        foreach (var s in signers)
        {
            var page = pdfDoc.GetPage(Math.Clamp(s.Order, 1, pdfDoc.GetNumberOfPages()));
            var img = ImageDataFactory.Create(Convert.FromBase64String(s.SignatureImage!));
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

            const float w = 150f,
                h = 50f,
                margin = 40f;
            canvas.AddImageWithTransformationMatrix(img, w, 0, 0, h, margin, margin, true);
        }

        // ② firma digital de plataforma (opcional: una por firmante si disponen de su propio cert)

        return dest.ToArray();
    }
}
