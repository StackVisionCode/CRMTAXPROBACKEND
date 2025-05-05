using AutoMapper;
using Common;
using Commands.SignatureEventType;
using MediatR;
using Infraestructure.Context;

namespace Handlers.SignatureEventType
{
    public class CreateSignatureEventTypeHandler : IRequestHandler<CreateSignatureEventTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateSignatureEventTypeHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(CreateSignatureEventTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Domains.Signatures.SignatureEventType>(request.SignatureEventType);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.DeleteAt = null;
            _context.SignatureEventTypes.Add(entity);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;
            return new ApiResponse<bool>(result, result ? "Signature Event Type created successfully" : "Failed to create Signature Event Type", result);
        }
    }
}
