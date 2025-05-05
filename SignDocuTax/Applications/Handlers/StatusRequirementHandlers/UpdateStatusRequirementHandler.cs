using AutoMapper;
using Common;
using Commands.StatusRequirement;

using MediatR;
using Infraestructure.Context;

namespace Handlers.StatusRequirementHandlers
{
    public class UpdateStatusRequirementHandler : IRequestHandler<UpdateStatusRequirementCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateStatusRequirementHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateStatusRequirementCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StatusRequirements.FindAsync(request.StatusRequirement.Id);
            if (entity == null)
                return new ApiResponse<bool>(false, "StatusRequirement not found");

            _mapper.Map(request.StatusRequirement, entity);
            var result = await _context.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>(result > 0, result > 0 ? "StatusRequirement updated successfully" : "Failed to update StatusRequirement", result > 0);

        }
    }
}
