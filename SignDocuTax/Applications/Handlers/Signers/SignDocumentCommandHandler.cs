using Common;
using MediatR;
using Infraestructure.Context;
using Commands.Signers;
using Microsoft.EntityFrameworkCore;
using Domain.Signatures;

namespace Handlers.Signers;

public class SignDocumentCommandHandler : IRequestHandler<SignDocumentCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;

    public SignDocumentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(SignDocumentCommands request, CancellationToken cancellationToken)
    {
        var signer = await _context.ExternalSigners
                   .Include(x => x.RequirementSignature)
                   .ThenInclude(rs => rs.EventSignatures)
                   .Include(x => x.RequirementSignature)
                   .ThenInclude(rs => rs.ExternalSigners)
                   .FirstOrDefaultAsync(x => x.Id == request.ExternalSignerId, cancellationToken);

        if (signer == null)
            return new ApiResponse<bool>(false, "Signer not found");

        if (signer.SignedDate != null)
            return new ApiResponse<bool>(false, "Signer has already signed");

        signer.SignedDate = DateTime.UtcNow;
        // Crear registro de firma
        signer.RequirementSignature.EventSignatures.Add(new EventSignature
        {
            ExternalSignerId = signer.Id,
            CreatedAt = DateTime.UtcNow
        });

        // Verificar si se completó la firma
        var requirement = signer.RequirementSignature;
        if (requirement != null)
        {
            var allSigned = requirement.ExternalSigners.All(x => x.SignedDate != null);
            var quantityMet = requirement.EventSignatures.Count >= requirement.Quantity;

            if (allSigned && quantityMet)
            {
                // Opcional: Marcar documento como firmado
                if (requirement.Document != null)
                {
                    requirement.Document.IsSigned = true;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, "Document signed successfully");

    }


}
