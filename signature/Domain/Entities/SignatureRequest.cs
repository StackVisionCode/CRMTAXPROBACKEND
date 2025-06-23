using Application.Helpers;
using Entities;

public class SignatureRequest : BaseEntity
{
    private readonly List<Signer> _signers = [];
    public IReadOnlyCollection<Signer> Signers => _signers.AsReadOnly();

    public Guid DocumentId { get; set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public SignatureStatus Status { get; set; }

    private SignatureRequest() { }

    public SignatureRequest(Guid documentId, Guid Id)
    {
        this.Id = Id;
        DocumentId = documentId;
        Status = SignatureStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddSigner(
        Guid signerId,
        Guid custId,
        string email,
        int order,
        int page,
        float x,
        float y,
        float width,
        float height,
        IntialEntity? initialEntity,
        FechaSigner? fechaSigner,
        string token
    ) => _signers.Add(new Signer(signerId, custId, email, order, Id, page, width, height, x, y, initialEntity, fechaSigner, token));

    public void ReceiveSignature(Guid signerId, string img, DigitalCertificate cert)
    {
        var s = _signers.Single(x => x.Id == signerId);
        s.MarkSigned(img, cert);

        if (_signers.All(x => x.Status == SignerStatus.Signed))
            Status = SignatureStatus.Completed;
    }

    public void MarkCompleted()
    {
        Status = SignatureStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
