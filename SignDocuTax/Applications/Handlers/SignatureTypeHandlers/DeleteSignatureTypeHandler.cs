using Commands.SignatureTypes;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SignatureType
{
    public class DeleteSignatureTypeHandler : IRequestHandler<DeleteSignatureTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;

        public DeleteSignatureTypeHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteSignatureTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.SignatureTypes.FirstOrDefaultAsync(x => x.Id == request.Id.Id, cancellationToken);

            if (entity == null)
                return new ApiResponse<bool>(false, "SignatureType not found", false);

            _dbContext.SignatureTypes.Remove(entity);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            return new ApiResponse<bool>(result, result ? "SignatureType deleted successfully" : "Failed to delete SignatureType", result);
        }
    }
}
