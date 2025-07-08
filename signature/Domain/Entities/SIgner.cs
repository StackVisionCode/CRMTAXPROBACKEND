using Application.Helpers;

namespace Domain.Entities;

public class Signer : BaseEntity
{
    public Guid? CustomerId { get; private set; }
    public string Email { get; private set; } = default!;
    public int Order { get; private set; }
    public SignerStatus Status { get; private set; }

    // Resultado de la firma
    public string? SignatureImage { get; private set; } // base64
    public DigitalCertificate? Certificate { get; private set; }
    public DateTime? SignedAtUtc { get; private set; }
    public string? ClientIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime? ConsentAgreedAtUtc { get; private set; }
    public string? ConsentText { get; private set; }
    public bool? ConsentButtonText { get; private set; }

    public Guid SignatureRequestId { get; private set; }
    public string Token { get; private set; } = default!;

    // --------------  cajas de firma (1-N) --------------
    private readonly List<SignatureBox> _boxes = new();
    public IReadOnlyCollection<SignatureBox> Boxes => _boxes.AsReadOnly();

    private Signer() { } // ←  requerido por EF

    public Signer(
        Guid signerId,
        Guid? customerId,
        string email,
        int order,
        Guid requestId,
        string token
    )
    {
        Id = signerId;
        CustomerId = customerId;
        Email = email;
        Order = order;
        SignatureRequestId = requestId;
        Token = token;
        Status = SignerStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddBoxes(IEnumerable<SignatureBox> boxes) => _boxes.AddRange(boxes);

    // ---------------- resultado de la firma ----------------
    internal void MarkSigned(
        string imageB64,
        DigitalCertificate cert,
        DateTime signedUtc,
        string ip,
        string ua,
        DateTime consentUtc,
        string? consentText,
        bool? consentButton
    )
    {
        SignatureImage = imageB64;
        Certificate = cert;
        SignedAtUtc = signedUtc;
        ClientIp = ip;
        UserAgent = ua;
        ConsentAgreedAtUtc = consentUtc;
        ConsentText = consentText;
        ConsentButtonText = consentButton;
        Status = SignerStatus.Signed;
        UpdatedAt = DateTime.UtcNow;

        // ─── Marcar cada SignatureBox ──────────────────────
        foreach (var box in _boxes)
            box.UpdatedAt = DateTime.UtcNow; // 1 sola línea
    }
}
