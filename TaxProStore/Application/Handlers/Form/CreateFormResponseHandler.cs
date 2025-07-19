using Application.Common;
using AutoMapper;
using Domain.Entity.Form;
using Infrastructure.Command.Form;
using Infrastructure.Context;
using MediatR;

public class CreateFormResponseHandler : IRequestHandler<CreateFormResponseCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;

    public CreateFormResponseHandler(TaxProStoreDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(CreateFormResponseCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<FormResponse>(request.Response);
        if (entity == null)
        {
            return new ApiResponse<bool>(false, "Invalid form response data.");
        }
        // Check if the form response already exists
            entity.Id = Guid.NewGuid(); // Ensure a new ID is set for the new response
        await _context.FormResponses.AddAsync(entity, cancellationToken);
        // Save changes to the databaser
        
        await _context.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, "Form response created successfully.");
    }
}
