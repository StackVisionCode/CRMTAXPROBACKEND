using AutoMapper;
using Commands.Signers;
using Common;
using Domains.Signers;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.Signers
{
    public class CreateExternalSignerCommandHandler : IRequestHandler<CreateExternalSignerCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateExternalSignerCommandHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(CreateExternalSignerCommand request, CancellationToken cancellationToken)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == request.CreateExternalSignerDto.ContactId, cancellationToken);

            if (contact == null)
            {
                return new ApiResponse<bool>(false, "Contact not found");
            }

            var signer = new ExternalSigner
        {
            ContactId = request.CreateExternalSignerDto.ContactId,
            RequirementSignatureId = request.CreateExternalSignerDto.RequirementSignatureId,
            DocumentId = request.CreateExternalSignerDto.DocumentId,
            InvitationSentDate = DateTime.UtcNow,
            SigningToken = Guid.NewGuid().ToString(),
            SignatureStatusId = 1
        };

            signer.CreatedAt = DateTime.UtcNow;
           signer.UpdatedAt = DateTime.UtcNow;

            _context.ExternalSigners.Add(signer);
            await _context.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>(true, "External Singer Succesfully");
        }
    }
}
