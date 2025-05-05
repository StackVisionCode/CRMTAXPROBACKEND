using Common;
using Commands.StatusRequirement;
using MediatR;
using Infraestructure.Context;

namespace Handlers.StatusRequirement
{
    public class DeleteStatusRequirementHandler : IRequestHandler<DeleteStatusRequirementCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;

        public DeleteStatusRequirementHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteStatusRequirementCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.StatusRequirements.FindAsync(request.StatusRequirement.Id);
            if (entity == null)
                return new ApiResponse<bool>(false, "StatusRequirement not found");

            _context.StatusRequirements.Remove(entity);
            var result = await _context.SaveChangesAsync(cancellationToken);
            if (result == 0)
                return new ApiResponse<bool>(false, "Failed to delete StatusRequirement");

            return new ApiResponse<bool>(true, "StatusRequirement deleted successfully", result > 0);
        }
    }
}
