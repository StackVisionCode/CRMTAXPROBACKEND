using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using SharedLibrary.Contracts;

public class CreateSignatureRequestHandler(
    SignatureDbContext db,
    IConfirmTokenService tokenSvc) : IRequestHandler<CreateSignatureRequestCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(CreateSignatureRequestCommand c, CancellationToken ct)
    {
        var dto = c.Payload;
        var req = new SignatureRequest(dto.DocumentId);

        foreach (var s in dto.Signers)
            req.AddSigner(s.CustomerId, s.Email, s.Order);

        db.Add(req);
        await db.SaveChangesAsync(ct);

        foreach (var signer in req.Signers)
        {
            var (tkn, exp) = tokenSvc.Generate(signer.Id, signer.Email);
             

        }

        return new ApiResponse<bool>(true, "Solicitud creada",true);
    }
}
