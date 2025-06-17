using Application.Helpers;
using Entities;

public class SignatureRequest : BaseEntity
{
    private readonly List<Signer> _signers = [];
    public IReadOnlyCollection<Signer> Signers => _signers.AsReadOnly();

    public Guid DocumentId { get; private set; }
    public SignatureStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SignatureRequest() { }

    public SignatureRequest(Guid documentId)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        Status = SignatureStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

     public void AddSigner(Guid custId, string email, int order)
        => _signers.Add(new Signer(custId, email, order, Id));

    public void ReceiveSignature(Guid signerId, string img, DigitalCertificate cert)
    {
        var s = _signers.Single(x => x.Id == signerId);
        s.MarkSigned(img, cert);

        if (_signers.All(x => x.Status == SignerStatus.Signed))
            Status = SignatureStatus.Completed;
    }
}