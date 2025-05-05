using AutoMapper;
using Commands.StatusRequirement;
using Common;
using Domains.Requirements;
using Infraestructure.Context;
using MediatR;

namespace Handlers.StatusRequirementHandlers;

public class CreateStatusRequirementHandler : IRequestHandler<CreateStatusRequirementCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    private readonly ILogger<CreateStatusRequirementHandler> _logger;

    public CreateStatusRequirementHandler(ApplicationDbContext context, IMapper mapper, ILogger<CreateStatusRequirementHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(CreateStatusRequirementCommand request, CancellationToken cancellationToken)
    {

        try
        {
            if (request.StatusRequirement == null)
                return new ApiResponse<bool>(false, "No data provided", false);

            if (string.IsNullOrEmpty(request.StatusRequirement.Name))
                return new ApiResponse<bool>(false, "Name is required", false);

            if (string.IsNullOrEmpty(request.StatusRequirement.Description))
                return new ApiResponse<bool>(false, "Description is required", false);

            var entity = _mapper.Map<Domains.Requirements.StatusRequirement>(request.StatusRequirement);

            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.DeleteAt = null;
            await _context.StatusRequirements.AddAsync(entity, cancellationToken);

            _logger.LogInformation("Creating Status Requirement: {FileName}", entity.Name);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation("Status Requirement created successfully: {FileName}", result);

            return new ApiResponse<bool>(result, result ? "Status Requirement created successfully" : "Failed to create document", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while validating request data");
            return new ApiResponse<bool>(false, "An error occurred while processing the request", false);
        }

    }
}

