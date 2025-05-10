using AutoMapper;
using Commands.SignatureTypes;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SignatureType
{
    public class UpdateSignatureTypeHandler : IRequestHandler<UpdateSignatureTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public UpdateSignatureTypeHandler(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateSignatureTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.SignatureTypes.FirstOrDefaultAsync(x => x.Id == request.SignatureType.Id, cancellationToken);

            if (entity == null)
                return new ApiResponse<bool>(false, "SignatureType not found", false);

            _mapper.Map(request.SignatureType, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            _dbContext.SignatureTypes.Update(entity);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            return new ApiResponse<bool>(result, result ? "SignatureType updated successfully" : "Failed to update SignatureType", result);
        }
    }
}
