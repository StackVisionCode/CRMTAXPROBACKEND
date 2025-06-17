
using Application.Helpers;

namespace Entities;


public class Signer: BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string Email { get; private set; }
    public int Order { get; private set; }
    public SignerStatus Status { get; private set; }
    public string? SignatureImage { get; private set; } // base64
    public DigitalCertificate? Certificate { get; private set; }
    public Guid SignatureRequestId { get; private set; }

    private Signer() { } // EF

    internal Signer(Guid custId, string email, int order, Guid reqId)
    {
        Id = Guid.NewGuid();
        CustomerId = custId;
        Email = email;
        Order = order;
        Status = SignerStatus.Pending;
        SignatureRequestId = reqId;
    }

    internal void MarkSigned(string img, DigitalCertificate cert)
    {
        SignatureImage = img;
        Certificate = cert;
        Status = SignerStatus.Signed;
    }
}
