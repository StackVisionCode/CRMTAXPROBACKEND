using Common;
using Commands.SignatureEventType;
using MediatR;
using Infraestructure.Context;

namespace Handlers.SignatureEventType
{
    public class DeleteSignatureEventTypeHandler : IRequestHandler<DeleteSignatureEventTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;

        public DeleteSignatureEventTypeHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteSignatureEventTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.SignatureEventTypes.FindAsync(request.SignatureEventType.Id);
            if (entity == null)
             return new ApiResponse<bool>(false, "SignatureEventType not found");
             
            _context.SignatureEventTypes.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
           
            var result = await _context.SaveChangesAsync(cancellationToken);
            if (result == 0)
                return new ApiResponse<bool>(false, "Failed to delete SignatureEventType");

            return new ApiResponse<bool>(true, "SignatureEventType deleted successfully", result > 0);
        }
    }
}
