using AutoMapper;
using Common;
using Commands.SignatureEventType;
using Domain.Signatures;
using MediatR;
using Infraestructure.Context;

namespace Handlers.SignatureEventType
{
    public class UpdateSignatureEventTypeHandler : IRequestHandler<UpdateSignatureEventTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateSignatureEventTypeHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateSignatureEventTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.SignatureEventTypes.FindAsync(request.SignatureEventType.Id);
            if (entity == null)
                     return new ApiResponse<bool>(false, "Signature Event not found");

            _mapper.Map(request.SignatureEventType, entity);
          
           var result = await _context.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>(result > 0, result > 0 ? "Signature Event updated successfully" : "Failed to update StatusRequirement", result > 0);

        }
    }
}
