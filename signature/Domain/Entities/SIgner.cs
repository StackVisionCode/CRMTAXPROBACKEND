using Application.Helpers;


namespace Entities;

public class Signer : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string? Email { get; private set; }
    public int Order { get; private set; }
    public SignerStatus Status { get; private set; }
    public string? SignatureImage { get; private set; } // base64
    public DigitalCertificate? Certificate { get; private set; }
    public IntialEntity? InitialEntity { get; private set; }
    public FechaSigner? FechaSigner { get; private set; }
    public Guid SignatureRequestId { get; private set; }
    public int PageNumber { get; private set; } // desde 1

    public float Width { get; private set; } // en puntos PDF
    public float Height { get; private set; } // en puntos PDF
    public float PositionX { get; private set; } // en puntos PDF
    public float PositionY { get; private set; }
    public string? Token { get; private set; }

    private Signer() { } // EF

    public Signer(
        Guid signerId,
        Guid custId,
        string email,
        int order,
        Guid reqId,
        int page,
        float width,
        float height,
        float x,
        float y,
        IntialEntity? InitialEntity,
        FechaSigner? fechaSigner,
        string token
    )
    {
        Id = signerId;
        CustomerId = custId;
        Email = email;
        Order = order;
        PageNumber = page;
        Width = width;
        Height = height;
        PositionX = x;
        PositionY = y;
        InitialEntity = InitialEntity;
        FechaSigner = fechaSigner;
        Token = token;
        Status = SignerStatus.Pending;
        SignatureRequestId = reqId;
        CreatedAt = DateTime.UtcNow;
    }

    internal void MarkSigned(string img, DigitalCertificate cert)
    {
        SignatureImage = img;
        Certificate = cert;
        Status = SignerStatus.Signed;
        UpdatedAt = DateTime.UtcNow;
    }
}
