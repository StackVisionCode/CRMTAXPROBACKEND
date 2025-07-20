using Application.Helpers;

namespace Domain.Entities;

public class SignatureRequest : BaseEntity
{
    private readonly List<Signer> _signers = [];
    public IReadOnlyCollection<Signer> Signers => _signers.AsReadOnly();
    public Guid? RejectedBySignerId { get; private set; }
    public string? RejectReason { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public Guid DocumentId { get; private set; }
    public SignatureStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private SignatureRequest() { } // EF

    public SignatureRequest(Guid documentId, Guid id)
    {
        Id = id;
        DocumentId = documentId;
        Status = SignatureStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AttachSigner(Signer signer) => _signers.Add(signer);

    public void ReceiveSignature(
        Guid signerId,
        string img,
        DigitalCertificate cert,
        DateTime signedAtUtc,
        string ip,
        string ua,
        DateTime consentUtc,
        string? consentText,
        bool? consentButton
    )
    {
        var s = _signers.Single(x => x.Id == signerId);
        s.MarkSigned(img, cert, signedAtUtc, ip, ua, consentUtc, consentText, consentButton);

        if (_signers.All(x => x.Status == SignerStatus.Signed))
            MarkCompleted();
    }

    public void MarkCompleted()
    {
        Status = SignatureStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRejected(Guid signerId, string? reason)
    {
        if (
            Status
            is SignatureStatus.Completed
                or SignatureStatus.Canceled
                or SignatureStatus.Expired
                or SignatureStatus.Rejected
        )
            return;

        Status = SignatureStatus.Rejected;
        RejectedBySignerId = signerId;
        RejectReason = reason;
        RejectedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
