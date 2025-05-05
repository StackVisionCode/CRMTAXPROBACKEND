using MediatR;
using Common;
using Infraestructure.Context;
using Domain.Signatures;

namespace Handlers.SignaturesHandlers;
public class CreateEventSignatureHandler : IRequestHandler<CreateEventSignatureCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;

    public CreateEventSignatureHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<bool>> Handle(CreateEventSignatureCommand request, CancellationToken cancellationToken)
    {
        var entity = new EventSignature
        {
            RequirementSignatureId = request.EventSign.RequirementSignatureId,
            AnswerRequirementId = request.EventSign.AnswerRequirementId,
            DocumentId = request.EventSign.DocumentId,
            TaxUserId = request.EventSign.TaxUserId,
            CompanyId = request.EventSign.CompanyId,
            IpAddress = request.EventSign.IpAddress,
            DeviceName = request.EventSign.DeviceName,
            DeviceOs = request.EventSign.DeviceOs,
            Browser = request.EventSign.Browser,
            SignatureDate = request.EventSign.SignatureDate,
            DigitalSignatureHash = request.EventSign.DigitalSignatureHash,
            SignatureImageUrl = request.EventSign.SignatureImageUrl,
            AuditTrailJson = request.EventSign.AuditTrailJson,
            IsValid = request.EventSign.IsValid,
            SignatureEventTypeId = request.EventSign.SignatureEventTypeId,
            TimestampToken = request.EventSign.TimestampToken,
            TimestampAuthority = request.EventSign.TimestampAuthority,
            SignatureLevel = request.EventSign.SignatureLevel,
            DocumentHashAtSigning = request.EventSign.DocumentHashAtSigning
        };

        _dbContext.EventSignatures.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, "EventSignature created successfully" );
    }
}
